using System.ComponentModel.DataAnnotations;

namespace Alquiler_Vehiculos.Dto.Oficina;

/// <summary>Coordenadas geográficas de la oficina (RF-11, RI-02).</summary>
public record LocalizacionDto(double Latitud, double Longitud);

/// <summary>Datos mínimos del trabajador responsable devueltos junto a la oficina.</summary>
public record ResponsableResumenDto(
    Guid Id,
    string Nombre,
    string Apellidos,
    string Email,
    string Telefono);

/// <summary>Respuesta de consulta de la oficina única (RF-11, RI-02).</summary>
public record OficinaResponseDto(
    string Nombre,
    string Direccion,
    string Ciudad,
    string CodigoPostal,
    string Telefono,
    string Email,
    LocalizacionDto Localizacion,
    DayOfWeek DiaInicio,
    DayOfWeek DiaFin,
    TimeOnly HoraApertura,
    TimeOnly HoraCierre,
    string HorarioFormateado,
    ResponsableResumenDto Responsable,
    DateTime UpdatedAt);

/// <summary>Petición de relevo de responsable de la oficina (CU-18).</summary>
public record CambiarResponsableDto {
    [Required] public Guid TrabajadorId { get; init; }
}
