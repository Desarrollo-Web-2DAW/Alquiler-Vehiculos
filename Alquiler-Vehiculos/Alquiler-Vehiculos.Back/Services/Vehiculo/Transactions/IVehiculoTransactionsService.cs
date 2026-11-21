using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Vehiculo;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Vehiculo.Transactions;

/// <summary>
/// Casos de uso transaccionales de la entidad Vehículo.
/// </summary>
public interface IVehiculoTransactionsService
{
    /// <summary>Elimina (borrado lógico) un vehículo y su foto asociada.</summary>
    Task<Result<VehiculoModel, DomainError>> DeleteAsync(string id, CancellationToken ct = default);

    /// <summary>Actualiza un vehículo y reemplaza su foto si ha cambiado.</summary>
    Task<Result<VehiculoModel, DomainError>> UpdateAsync(
        string id, VehiculoModel datos, CancellationToken ct = default);

    /// <summary>Restaura un vehículo y su foto asociada.</summary>
    Task<Result<VehiculoModel, DomainError>> RestoreAsync(string id, CancellationToken ct = default);
}
