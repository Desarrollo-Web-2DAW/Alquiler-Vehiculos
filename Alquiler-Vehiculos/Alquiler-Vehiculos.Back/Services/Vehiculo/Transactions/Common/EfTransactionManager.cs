using Alquiler_Vehiculos.Data.Vehiculo;
using Alquiler_Vehiculos.Errors.Common;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Vehiculo.Transactions.Common;

/// <summary>
/// Implementación de <see cref="ITransactionManager"/> sobre EF Core y PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// Es la única pieza de la solución que conoce el mecanismo transaccional de EF Core
/// (<see cref="DatabaseFacade.BeginTransactionAsync(CancellationToken)"/>); el dominio
/// y los servicios solo ven <see cref="ITransactionManager"/>.
/// </para>
/// <para>
/// Modo mixto: los repositorios pueden guardar con su propio <c>SaveChangesAsync</c>
/// (quedan dentro del límite transaccional compartido) y, además, antes del
/// <c>COMMIT</c> se guarda cualquier cambio rastreado que nadie haya persistido.
/// Los dos estilos conviven en la misma unidad de trabajo.
/// </para>
/// </remarks>
/// <param name="context">Contexto de EF Core</param>
/// <param name="logger">Logger del gestor transaccional.</param>
public sealed class EfTransactionManager(
    PostgreDbContext context,
    ILogger<EfTransactionManager> logger) : ITransactionManager
{
    /// <inheritdoc cref="ITransactionManager.ExecuteAsync{T}" />
    /// <remarks>
    /// Se considera éxito únicamente un <see cref="Result{T,E}"/> en estado exitoso:
    /// un fallo de negocio no lanza excepción y por eso se revierte de forma explícita.
    /// </remarks>
    public async Task<Result<T, DomainError>> ExecuteAsync<T>(
        Func<Task<Result<T, DomainError>>> operation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (context.Database.CurrentTransaction is not null)
        {
            logger.LogDebug("Ya existe una transacción ambiente: se reutiliza en lugar de abrir otra.");
            return await operation();
        }

        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        try
        {
            var result = await operation();

            if (result.IsFailure)
            {
                await transaction.RollbackAsync(ct);
                logger.LogWarning(
                    "Transacción revertida por fallo de negocio: {Error}", result.Error);
                return result;
            }
            
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return result;
        }
        catch
        {
            await RevertirAsync(transaction);
            throw;
        }
    }

    /// <summary>
    /// Revierte la transacción sin enmascarar la excepción original que provocó el fallo.
    /// </summary>
    /// <param name="transaction">Transacción en curso.</param>
    private async Task RevertirAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (Exception rollbackError)
        {
            logger.LogError(rollbackError, "No se pudo revertir la transacción.");
        }
    }
}
