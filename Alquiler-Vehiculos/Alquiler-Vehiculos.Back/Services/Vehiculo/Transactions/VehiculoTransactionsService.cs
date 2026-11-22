using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;
using Alquiler_Vehiculos.Services.Common;
using Alquiler_Vehiculos.Services.Taller;
using Alquiler_Vehiculos.Services.Vehiculo.Transactions.Common;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Vehiculo.Transactions;

/// <summary>
/// Casos de uso transaccionales de Vehículo.
/// No conoce EF Core: delega el límite transaccional en <see cref="ITransactionManager"/>.
    /// Los repositorios guardan cambios dentro del límite transaccional compartido.
/// </summary>
public sealed class VehiculoTransactionsService(
    IVehiculoRepository vehiculoRepository,
    IFotoRepository fotoRepository,
    IParteTallerService parteTallerService,
    ITransactionManager transactionManager,
    ILogger<VehiculoTransactionsService> logger) : IVehiculoTransactionsService
{
    /// <inheritdoc />
    public Task<Result<VehiculoModel, DomainError>> DeleteAsync(string id, CancellationToken ct = default)
        => transactionManager.ExecuteAsync<VehiculoModel>(async () =>
        {
            logger.LogInformation("Eliminando vehículo {Id}", id);
            
            var vehiculo = await vehiculoRepository.GetById(id);
            if (vehiculo is null)
            {
                return Result.Failure<VehiculoModel, DomainError>(VehiculoError.NotFoundVehiculo(id));
            }

            // RF-01: solo se da de baja un vehículo Disponible. Reservas vivas: pendiente de integrar.
            if (vehiculo.Estado != EstadoVehiculo.Disponible)
            {
                return Result.Failure<VehiculoModel, DomainError>(VehiculoError.NoDisponibleParaBaja(vehiculo.Estado));
            }

            // RF-01 y RF-08: tampoco se da de baja con un parte de taller abierto.
            var partesAbiertos = await parteTallerService.TienePartesAbiertosAsync(id, ct);
            if (partesAbiertos.IsFailure)
            {
                return Result.Failure<VehiculoModel, DomainError>(partesAbiertos.Error);
            }

            if (partesAbiertos.Value)
            {
                return Result.Failure<VehiculoModel, DomainError>(VehiculoError.VehiculoConPartesAbiertos(id));
            }

            if (!await vehiculoRepository.SoftDelete(id))
            {
                return Result.Failure<VehiculoModel, DomainError>(VehiculoError.BorradoError());
            }

            // RF-48: la baja del vehículo conserva sus fotos para no romper el histórico.
            return Result.Success<VehiculoModel, DomainError>(vehiculo);
        }, ct);

    /// <inheritdoc />
    public Task<Result<VehiculoModel, DomainError>> UpdateAsync(
        string id, VehiculoModel datos, CancellationToken ct = default)
        => transactionManager.ExecuteAsync(async () =>
        {
            logger.LogInformation("Actualizando vehículo {Id}", id);

            var actual = await vehiculoRepository.GetById(id);
            if (actual is null)
            {
                return Result.Failure<VehiculoModel, DomainError>(VehiculoError.NotFoundVehiculo(id));
            }
            
            var fotoAnterior = actual.FotoFk;

            var actualizado = await vehiculoRepository.Update(id, datos);
            if (actualizado is null)
            {
                return Result.Failure<VehiculoModel, DomainError>(VehiculoError.NotFoundVehiculo(id));
            }

            if (!string.IsNullOrEmpty(fotoAnterior) && fotoAnterior != datos.FotoFk)
            {
                var fotoEliminada = await fotoRepository.SoftDelete(fotoAnterior);
                if (!fotoEliminada)
                {
                    return Result.Failure<VehiculoModel, DomainError>(
                        new BusinessRuleError(
                            "ACTUALIZACION_FOTO",
                            "No se pudo actualizar el vehículo: fallo al eliminar la foto anterior."));
                }
            }

            return Result.Success<VehiculoModel, DomainError>(actualizado);
        }, ct);

    /// <inheritdoc />
    public Task<Result<VehiculoModel, DomainError>> RestoreAsync(string id, CancellationToken ct = default)
        => transactionManager.ExecuteAsync(async () =>
        {
            logger.LogInformation("Restaurando vehículo {Id}", id);

            var restaurado = await vehiculoRepository.Restore(id);
            if (restaurado is null)
            {
                return Result.Failure<VehiculoModel, DomainError>(VehiculoError.NotFoundVehiculo(id));
            }

            if (!string.IsNullOrEmpty(restaurado.FotoFk))
            {
                var fotoRestaurada = await fotoRepository.Restore(restaurado.FotoFk);
                if (fotoRestaurada == null)
                {
                    return Result.Failure<VehiculoModel, DomainError>(
                        new BusinessRuleError("RESTAURACION_FOTO", "No se pudo restaurar la foto asociada al vehículo."));
                }
            }

            return Result.Success<VehiculoModel, DomainError>(restaurado);
        }, ct);
}
