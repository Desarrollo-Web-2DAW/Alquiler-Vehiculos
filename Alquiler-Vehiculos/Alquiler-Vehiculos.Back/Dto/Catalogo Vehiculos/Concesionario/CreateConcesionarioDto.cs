namespace Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Concesionario;

/// <summary>
/// Datos de entrada para crear un concesionario.
/// </summary>
public record CreateConcesionarioDto {
    /// <summary>Nombre del concesionario.</summary>
    public string Nombre { get; set; } = string.Empty;
    /// <summary>Ciudad donde se encuentra el concesionario.</summary>
    public string Ciudad { get; set; } = string.Empty;
    /// <summary>Teléfono de contacto del concesionario.</summary>
    public string Telefono { get; set; } = string.Empty;
    /// <summary>Correo electrónico de contacto del concesionario.</summary>
    public string Email { get; set; } = string.Empty;
};
