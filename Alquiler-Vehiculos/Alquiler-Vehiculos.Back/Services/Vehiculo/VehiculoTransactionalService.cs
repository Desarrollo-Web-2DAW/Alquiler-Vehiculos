using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Repositories.Storage;
using CSharpFunctionalExtensions;
using Serilog;

namespace Alquiler_Vehiculos.Services.Vehiculo;

/// <summary>
/// Servicio encargado de gestionar las operaciones transaccionales complejas 
/// y el almacenamiento o gestión de fotos asociado para la entidad Vehículo.
/// </summary>
public class VehiculoTransactionsService(
    PostgreDbContext context,
    IFotoRepository fotoRepository)
{
    /// <summary>
    /// Elimina un Vehículo de forma transaccional junto con su foto asociada (soft delete).
    /// Si falla el borrado del vehículo o el borrado lógico de la foto, se ejecuta un Rollback completo.
    /// </summary>
    /// <param name="vehiculo">Instancia del Vehículo a eliminar.</param>
    /// <returns>Un resultado que indica <c>true</c> si fue exitoso, o un <see cref="DomainError"/> si falla.</returns>
    public async Task<Result<bool, DomainError>> Delete(VehiculoModel vehiculo)
    {
        Log.Information("Iniciando proceso transaccional de borrado para el Vehículo con ID {Id}", vehiculo.Id);

        var fotoId = vehiculo.FotoFk;

        await using var transaction = await context.Database.BeginTransactionAsync();
        try {
            context.Vehiculos.Attach(vehiculo);
            context.Vehiculos.Remove(vehiculo);
            await context.SaveChangesAsync();
            
            var fotoDeleted = await fotoRepository.SoftDelete(fotoId);
            if (!fotoDeleted) {
                Log.Warning("Fallo al realizar borrado lógico de la foto asociada {FotoId}. Ejecutando Rollback de BDD.", fotoId);
                await transaction.RollbackAsync();
                return Result.Failure<bool, DomainError>(new BusinessRuleError("No se pudo eliminar la foto asociada al vehículo. Operación cancelada."));
            }

            await transaction.CommitAsync();

            Log.Information("Vehículo {Id} y su foto eliminados correctamente de forma transaccional.", vehiculo.Id);
            return Result.Success<bool, DomainError>(true);
        }
        catch (Exception ex) {
            await transaction.RollbackAsync();
            Log.Error(ex, "Error crítico al intentar eliminar el Vehículo {Id}", vehiculo.Id);
            return Result.Failure<bool, DomainError>(new BusinessRuleError($"Error en el proceso transaccional de eliminación: {ex.Message}"));
        }
    }
    
    /// <summary>
    /// Actualiza la información de un Vehículo de forma transaccional gestionando el reemplazo de su foto.
    /// Si la foto anterior es distinta de la nueva foto, se aplica soft-delete a la foto anterior. Si falla, hace Rollback.
    /// </summary>
    /// <param name="vehiculoToUpdate">Instancia del Vehículo con los datos actualizados.</param>
    /// <param name="oldFotoId">ID de la foto anterior que tenía el vehículo antes de actualizar.</param>
    /// <returns>El Vehículo actualizado, o un <see cref="DomainError"/> si ocurre algún fallo.</returns>
    public async Task<Result<VehiculoModel, DomainError>> Update(VehiculoModel vehiculoToUpdate, string oldFotoId)
    {
        Log.Information("Iniciando actualización transaccional mixta para el Vehículo con ID {Id}", vehiculoToUpdate.Id);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try {
            context.Vehiculos.Update(vehiculoToUpdate);
            await context.SaveChangesAsync();
            
            if (oldFotoId != vehiculoToUpdate.FotoFk) {
                var fotoDeleted = await fotoRepository.SoftDelete(oldFotoId);
                if (!fotoDeleted) {
                    Log.Warning("Fallo al realizar borrado lógico de la foto anterior {OldFotoId}. Ejecutando Rollback de BDD.", oldFotoId);
                    await transaction.RollbackAsync();
                    return Result.Failure<VehiculoModel, DomainError>(new BusinessRuleError("No se pudo actualizar el vehículo debido a un fallo al eliminar la foto anterior."));
                }
            }

            await transaction.CommitAsync();

            Log.Information("Vehículo {Id} actualizado correctamente con gestión transaccional mixta de foto.", vehiculoToUpdate.Id);
            return Result.Success<VehiculoModel, DomainError>(vehiculoToUpdate);
        }
        catch (Exception ex) {
            await transaction.RollbackAsync();
            Log.Error(ex, "Error crítico al intentar actualizar el Vehículo {Id}", vehiculoToUpdate.Id);
            return Result.Failure<VehiculoModel, DomainError>(new BusinessRuleError($"Error en el proceso transaccional de actualización: {ex.Message}"));
        }
    }
}