using Alquiler_Vehiculos.Models.Taller;

namespace Alquiler_Vehiculos.Repositories.Taller;

/// <summary>
/// Acceso a datos de los partes de taller (RF-08).
/// La implementación actual vive en memoria; la de EF Core llega cuando se fije
/// la base de datos transaccional junto al módulo de vehículos, y allí la regla
/// «un solo parte abierto por vehículo» se garantizará con un índice único parcial.
/// </summary>
public interface IParteTallerRepository {
    /// <summary>
    /// Añade un parte nuevo en estado <c>Abierto</c>. La comprobación de que el
    /// vehículo no tenga ya un parte abierto es atómica: devuelve false si lo tiene.
    /// </summary>
    /// <param name="parte">Parte a guardar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>True si se ha guardado; false si ya hay un parte abierto para ese vehículo.</returns>
    Task<bool> AgregarAsync(ParteTaller parte, CancellationToken ct = default);

    /// <summary>Busca un parte por su identificador.</summary>
    /// <param name="id">ID del parte.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El parte, o null si no existe.</returns>
    Task<ParteTaller?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Busca el parte abierto de un vehículo, si lo tiene.</summary>
    /// <param name="vehiculoId">Identificador del vehículo.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El parte abierto, o null si el vehículo no está en el taller.</returns>
    Task<ParteTaller?> ObtenerAbiertoPorVehiculoAsync(string vehiculoId, CancellationToken ct = default);

    /// <summary>
    /// Lista los partes abiertos por fecha de entrada: son los vehículos que están
    /// en el taller ahora mismo con su fecha de entrada (CU-08).
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Partes abiertos ordenados por fecha de entrada.</returns>
    Task<IReadOnlyList<ParteTaller>> ObtenerAbiertosAsync(CancellationToken ct = default);

    /// <summary>Lista el historial completo de partes de un vehículo.</summary>
    /// <param name="vehiculoId">Identificador del vehículo.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Partes del vehículo ordenados por fecha de entrada.</returns>
    Task<IReadOnlyList<ParteTaller>> ObtenerPorVehiculoAsync(string vehiculoId, CancellationToken ct = default);

    /// <summary>Guarda los cambios de un parte existente (por ejemplo, al cerrarlo).</summary>
    /// <param name="parte">Parte con los cambios.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task ActualizarAsync(ParteTaller parte, CancellationToken ct = default);
}
