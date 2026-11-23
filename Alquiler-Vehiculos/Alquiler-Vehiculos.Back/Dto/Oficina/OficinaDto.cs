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

/// <summary>Respuesta de consulta y actualización de la oficina única (RF-11, RI-02).</summary>
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

/// <summary>Datos editables de la oficina y su horario semanal (RF-11).</summary>
public record UpdateOficinaDto {
    [Required, MaxLength(150)] public string Nombre { get; init; } = string.Empty;
    [Required, MaxLength(250)] public string Direccion { get; init; } = string.Empty;
    [Required, MaxLength(100)] public string Ciudad { get; init; } = string.Empty;
    [Required, MaxLength(10)] public string CodigoPostal { get; init; } = string.Empty;
    [Required, MaxLength(20)] public string Telefono { get; init; } = string.Empty;
    [Required, EmailAddress, MaxLength(150)] public string Email { get; init; } = string.Empty;
    public double Latitud { get; init; }
    public double Longitud { get; init; }
    public DayOfWeek DiaInicio { get; init; } = DayOfWeek.Monday;
    public DayOfWeek DiaFin { get; init; } = DayOfWeek.Friday;
    public TimeOnly HoraApertura { get; init; }
    public TimeOnly HoraCierre { get; init; }
}

/// <summary>Petición de relevo de responsable de la oficina (CU-18).</summary>
public record CambiarResponsableDto {
    [Required] public Guid TrabajadorId { get; init; }
}
