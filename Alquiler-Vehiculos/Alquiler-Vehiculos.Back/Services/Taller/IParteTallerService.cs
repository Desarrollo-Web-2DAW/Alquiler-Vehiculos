using Alquiler_Vehiculos.Dto.Taller;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Taller;

/// <summary>
/// Contrato del servicio de partes de taller (RF-08).
/// Todas las operaciones devuelven <see cref="Result{T, TError}"/>: el error de
/// dominio se convierte en HTTP en el controlador con <c>ToHttpResult()</c>.
/// </summary>
public interface IParteTallerService {
    /// <summary>
    /// Abre un parte para la entrada de un vehículo al taller (RF-08, CU-07).
    /// Solo un trabajador <c>Mecanico</c> activo puede hacerlo (RF-12) y el
    /// vehículo no puede tener ya un parte abierto.
    /// </summary>
    /// <param name="tramitador">Trabajador que tramita el parte.</param>
    /// <param name="dto">Vehículo y descripción del trabajo.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El parte abierto, o el error de dominio.</returns>
    Task<Result<ParteTallerDto, DomainError>> AbrirParteAsync(
        Trabajador tramitador, AbrirParteDto dto, CancellationToken ct = default);

    /// <summary>
    /// Cierra un parte al terminar la reparación (RF-08, CU-07). Es la única
    /// transición posible: de <c>Abierto</c> a <c>Cerrado</c>, y no se reabren partes.
    /// </summary>
    /// <param name="parteId">ID del parte.</param>
    /// <param name="tramitador">Trabajador que tramita el cierre.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El parte cerrado, o el error de dominio.</returns>
    Task<Result<ParteTallerDto, DomainError>> CerrarParteAsync(
        Guid parteId, Trabajador tramitador, CancellationToken ct = default);

    /// <summary>Consulta un parte por su identificador.</summary>
    /// <param name="id">ID del parte.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El parte, o el error de dominio (404 si no existe).</returns>
    Task<Result<ParteTallerDto, DomainError>> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Vehículos que están en el taller ahora mismo con su fecha de entrada (CU-08).
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Partes abiertos por fecha de entrada.</returns>
    Task<Result<IReadOnlyList<ParteTallerDto>, DomainError>> ListarAbiertosAsync(CancellationToken ct = default);

    /// <summary>Historial completo de partes de un vehículo.</summary>
    /// <param name="vehiculoId">Identificador del vehículo.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Partes del vehículo por fecha de entrada.</returns>
    Task<Result<IReadOnlyList<ParteTallerDto>, DomainError>> ListarPorVehiculoAsync(
        string vehiculoId, CancellationToken ct = default);

    /// <summary>
    /// Indica si un vehículo tiene un parte abierto: es la comprobación que pide
    /// RF-01 antes de dar de baja un vehículo («sin partes abiertos», si no, 409).
    /// </summary>
    /// <param name="vehiculoId">Identificador del vehículo.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>True si el vehículo está en el taller con un parte abierto.</returns>
    Task<Result<bool, DomainError>> TienePartesAbiertosAsync(string vehiculoId, CancellationToken ct = default);
}
