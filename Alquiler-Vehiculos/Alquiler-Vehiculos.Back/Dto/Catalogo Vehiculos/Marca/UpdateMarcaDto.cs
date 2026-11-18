namespace Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Marca;

/// <summary>
/// Datos de entrada para actualizar una marca y sus modelos disponibles.
/// </summary>
public record UpdateMarcaDto {
    /// <summary>Nombre de la marca.</summary>
    public string Nombre { get; set; } = string.Empty;
    /// <summary>País de la marca.</summary>
    public string Pais { get; set; } = string.Empty;
    /// <summary>Dirección del sitio web de la marca.</summary>
    public string Web { get; set; } = string.Empty;
    /// <summary>Nombres de los modelos disponibles de la marca.</summary>
    public ICollection<string> ModelosDisponibles { get; set; } = [];
};
