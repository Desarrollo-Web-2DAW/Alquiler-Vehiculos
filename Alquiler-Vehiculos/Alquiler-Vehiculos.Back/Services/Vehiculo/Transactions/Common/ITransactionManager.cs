using Alquiler_Vehiculos.Errors.Common;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Vehiculo.Transactions.Common;

/// <summary>
/// Abstracción de la unidad de trabajo transaccional.
/// El dominio y los servicios dependen de esta interfaz, nunca de EF Core.
/// </summary>
public interface ITransactionManager
{
    /// <summary>
    /// Ejecuta <paramref name="operation"/> dentro de una transacción.
    /// Si el resultado es un fallo, o se lanza una excepción, se hace rollback
    /// y no se persiste ningún cambio. Si es éxito, se guarda y se confirma.
    /// </summary>
    /// <typeparam name="T">Tipo del valor de éxito.</typeparam>
    /// <param name="operation">Caso de uso a ejecutar (solo muta el grafo, no guarda).</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<Result<T, DomainError>> ExecuteAsync<T>(
        Func<Task<Result<T, DomainError>>> operation,
        CancellationToken ct = default);
}
