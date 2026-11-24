using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Storage;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;
using Alquiler_Vehiculos.Services.Common;
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
    /// <inheritdoc cref="IVehiculoTransactionsService.DeleteAsync" />
    /// <remarks>
    /// Enfoque mixto de concurrencia: la fase 1 solo <b>lee</b> para poder dar un mensaje
    /// claro al usuario, pero la garantía la da la fase 2, un <c>UPDATE ... WHERE Estado</c>
    /// que o afecta a una fila o a ninguna. La fase 3 (segunda escritura) queda vacía en este
    /// caso de uso porque la baja conserva la foto. Sigue pendiente integrar la comprobación
    /// de reservas vivas cuando exista el contrato del módulo de reservas (RF-01).
    /// </remarks>
    public Task<Result<VehiculoModel, DomainError>> DeleteAsync(string id, CancellationToken ct = default)
        => transactionManager.ExecuteAsync<VehiculoModel>(async () =>
        {
            logger.LogInformation("Eliminando vehículo {Id}", id);

            // ── Fase 1: validación optimista (solo lectura) ─────────────────────
            // Cortesía: sirve para responder "no está disponible" con su estado real.
            // No es la garantía, porque entre este SELECT y el UPDATE puede cambiar.
            var vehiculo = await vehiculoRepository.GetById(id);
            if (vehiculo is null)
            {
                return Result.Failure<VehiculoModel, DomainError>(VehiculoError.NotFoundVehiculo(id));
            }

            if (vehiculo.Estado != EstadoVehiculo.Disponible)
            {
                return Result.Failure<VehiculoModel, DomainError>(VehiculoError.NoDisponibleParaBaja(vehiculo.Estado));
            }

            var partesAbiertos = await parteTallerService.TienePartesAbiertosAsync(id, ct);
            if (partesAbiertos.IsFailure)
            {
                return Result.Failure<VehiculoModel, DomainError>(partesAbiertos.Error);
            }

            if (partesAbiertos.Value)
            {
                return Result.Failure<VehiculoModel, DomainError>(VehiculoError.VehiculoConPartesAbiertos(id));
            }

            // ── Fase 2: compare-and-set (la garantía real) ──────────────────────
            if (!await vehiculoRepository.SoftDeleteSiEstado(id, EstadoVehiculo.Disponible))
            {
                // 0 filas = otro proceso cambió el estado o dio de baja el vehículo
                // entre la fase 1 y la fase 2. Se devuelve un fallo de negocio y el
                // gestor transaccional revierte: no es un error técnico.
                logger.LogWarning(
                    "Baja condicional sin efecto para el vehículo {Id}: cambió su estado", id);

                var ultimo = await vehiculoRepository.GetById(id);
                if (ultimo is null || !ultimo.Activo)
                {
                    return Result.Failure<VehiculoModel, DomainError>(VehiculoError.NotFoundVehiculo(id));
                }

                return Result.Failure<VehiculoModel, DomainError>(
                    VehiculoError.NoDisponibleParaBaja(ultimo.Estado));
            }

            return Result.Success<VehiculoModel, DomainError>(vehiculo);
        }, ct);

    /// <inheritdoc cref="IVehiculoTransactionsService.UpdateAsync" />
    public async Task<Result<VehiculoModel, DomainError>> UpdateAsync(
        string id, VehiculoModel datos, CancellationToken ct = default)
    {
        try {
            return await transactionManager.ExecuteAsync(async () =>
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

                // Idempotencia: si la foto anterior ya no existe o ya estaba de baja,
                // el estado deseado (que no quede activa) ya se cumple. Devolver un
                // error aquí sería un conflicto falso, porque el vehículo ya apunta
                // a la foto nueva y la foto nueva está activa.
                if (!fotoEliminada && !await EstaDeBajaAsync(fotoAnterior))
                {
                    return Result.Failure<VehiculoModel, DomainError>(
                        FotoError.NoSePudoDarDeBaja(fotoAnterior));
                }
            }

            return Result.Success<VehiculoModel, DomainError>(actualizado);
            }, ct);
        } catch (DbUpdateException ex) when (ex.InnerException is PostgresException {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_Vehiculos_Matrícula"
        }) {
            // Se traduce después de que el gestor transaccional haya deshecho la operación.
            // La comprobación previa del servicio no evita dos escrituras simultáneas (RF-02).
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

    /// <summary>
    /// Comprueba si una foto ya no está activa: no existe, tiene <c>Activo = false</c>
    /// o arrastra fecha de borrado lógico.
    /// </summary>
    /// <param name="nombre">Clave de la foto.</param>
    /// <returns><c>true</c> si el estado deseado (foto no activa) ya se cumple.</returns>
    private async Task<bool> EstaDeBajaAsync(string nombre)
    {
        var foto = await fotoRepository.GetById(nombre);
        return foto is null || !foto.Activo || foto.DeletedAt is not null;
    }
}
