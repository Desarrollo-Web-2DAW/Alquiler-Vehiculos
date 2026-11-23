using Alquiler_Vehiculos.Dto.Oficina;
using Alquiler_Vehiculos.Errors.Common;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Oficina;

/// <summary>
/// Contrato de negocio de la oficina única (RF-11, CU-18, CU-22).
/// </summary>
public interface IOficinaService {
    /// <summary>Obtiene los datos, horario y responsable actual de la oficina única.</summary>
    Task<Result<OficinaResponseDto, DomainError>> ObtenerAsync(CancellationToken ct = default);

    /// <summary>Actualiza los datos de contacto y el horario semanal de la oficina.</summary>
    Task<Result<OficinaResponseDto, DomainError>> ActualizarAsync(UpdateOficinaDto dto, CancellationToken ct = default);

    /// <summary>Nombra a un nuevo trabajador activo de Operaciones como responsable (CU-18).</summary>
    Task<Result<OficinaResponseDto, DomainError>> CambiarResponsableAsync(CambiarResponsableDto dto, CancellationToken ct = default);

    /// <summary>Enganche para el módulo de Trabajadores: impide dar de baja al responsable actual (CU-18).</summary>
    Task<bool> EsResponsableAsync(Guid trabajadorId, CancellationToken ct = default);

    /// <summary>Enganche para el módulo de Reservas: comprueba si una fecha/hora está dentro del horario (CU-22).</summary>
    Task<bool> EstaAbiertaAsync(DateTime fechaHora, CancellationToken ct = default);
}
