using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Storage;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;
using Alquiler_Vehiculos.Services.Common;
using Alquiler_Vehiculos.Services.Storage;
using Alquiler_Vehiculos.Services.Taller;
using Alquiler_Vehiculos.Services.Vehiculo.Transactions.Common;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Alquiler_Vehiculos.Services.Vehiculo.Transactions;

/// <summary>
/// Casos de uso transaccionales de Vehículo.
/// Delega el límite transaccional en <see cref="ITransactionManager"/> y traduce
/// la restricción de matrícula duplicada a un conflicto de dominio.
/// Los repositorios guardan cambios dentro del límite transaccional compartido.
/// </summary>
public sealed class VehiculoTransactionsService(
    IVehiculoRepository vehiculoRepository,
    IFotoRepository fotoRepository,
    IParteTallerService parteTallerService,
    ITransactionManager transactionManager,
    ILogger<VehiculoTransactionsService> logger) : IVehiculoTransactionsService
{

    /// <inheritdoc cref="IVehiculoTransactionsService.UpdateAsync" />
    public async Task<Result<UpdateVehiculoResult, DomainError>> UpdateAsync(
        string id, VehiculoModel datos, CancellationToken ct = default)
    {
        try {
            return await transactionManager.ExecuteAsync(async () =>
            {
                logger.LogInformation("Actualizando vehículo {Id}", id);

                var actual = await vehiculoRepository.GetByIdForUpdate(id, ct);
                if (actual is null)
                    return Result.Failure<UpdateVehiculoResult, DomainError>(VehiculoError.NotFoundVehiculo(id));
                if (!actual.Activo)
                    return Result.Failure<UpdateVehiculoResult, DomainError>(VehiculoError.NoActualizable(id));
                if (datos.Matrícula != actual.Matrícula &&
                    await vehiculoRepository.ExistByMatricula(datos.Matrícula))
                    return Result.Failure<UpdateVehiculoResult, DomainError>(
                        VehiculoError.VehiculoYaExiste(datos.Matrícula));

                var fotoNueva = datos.Foto;
                await fotoRepository.Create(fotoNueva);
                var matriculaAnterior = actual.Matrícula;
                var fotoAnterior = actual.FotoFk;
                var actualizado = vehiculoRepository.ApplyChanges(actual, datos);

                if (!string.IsNullOrEmpty(fotoAnterior) && fotoAnterior != fotoNueva.Nombre)
                    await fotoRepository.SoftDelete(fotoAnterior);

                return Result.Success<UpdateVehiculoResult, DomainError>(
                    new(actualizado, matriculaAnterior));
            }, ct);
        } catch (DbUpdateException ex) when (ex.InnerException is PostgresException {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_Vehiculos_Matrícula"
        }) {
          
            return VehiculoError.VehiculoYaExiste(datos.Matrícula);
        }
    }

    /// <inheritdoc cref="IVehiculoTransactionsService.RestoreAsync" />
    /// <remarks>
    /// Enfoque mixto de concurrencia: la fase 1 solo <b>lee</b> para responder con el
    /// estado real, y la garantía la da la fase 2, un <c>UPDATE ... WHERE Estado</c> que
    /// solo reactiva un vehículo retirado. Un vehículo reservado, alquilado o en
    /// mantenimiento no se libera por esta vía.
    /// </remarks>
    public Task<Result<VehiculoModel, DomainError>> RestoreAsync(string id, CancellationToken ct = default)
        => transactionManager.ExecuteAsync(async () =>
        {
            logger.LogInformation("Restaurando vehículo {Id}", id);

            // ── Fase 1: validación optimista (solo lectura) ─────────────────────
            var vehiculo = await vehiculoRepository.GetById(id);
            if (vehiculo is null)
            {
                return Result.Failure<VehiculoModel, DomainError>(VehiculoError.NotFoundVehiculo(id));
            }

            if (vehiculo.Activo || vehiculo.Estado != EstadoVehiculo.Retirado)
            {
                return Result.Failure<VehiculoModel, DomainError>(
                    VehiculoError.NoRetiradoParaRestaurar(vehiculo.Estado));
            }

            // ── Fase 2: compare-and-set (la garantía real) ──────────────────────
            var restaurado = await vehiculoRepository.Restore(id);
            if (restaurado is null)
            {
                // 0 filas = otro proceso cambió el estado entre la fase 1 y la fase 2.
                var ultimo = await vehiculoRepository.GetById(id);
                if (ultimo is null)
                {
                    return Result.Failure<VehiculoModel, DomainError>(VehiculoError.NotFoundVehiculo(id));
                }

                return Result.Failure<VehiculoModel, DomainError>(
                    VehiculoError.NoRetiradoParaRestaurar(ultimo.Estado));
            }

            if (!string.IsNullOrEmpty(restaurado.FotoFk))
            {
                var fotoRestaurada = await fotoRepository.Restore(restaurado.FotoFk);
                if (fotoRestaurada == null)
                {
                    return Result.Failure<VehiculoModel, DomainError>(
                        FotoError.NoSePudoRestaurar(restaurado.FotoFk));
                }
            }
            return Result.Success<VehiculoModel, DomainError>(restaurado);
        }, ct);

}
