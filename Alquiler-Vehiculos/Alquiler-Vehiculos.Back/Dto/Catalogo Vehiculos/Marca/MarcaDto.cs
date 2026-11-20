
namespace Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Marca;

/// <summary>
/// Datos de una marca y sus modelos disponibles devueltos por la API.
/// </summary>
public record MarcaDto {
    /// <summary>ObjectId de la marca representado como cadena hexadecimal de 24 caracteres.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Nombre de la marca.</summary>
    public string Nombre { get; set; } = string.Empty;
    /// <summary>País de la marca.</summary>
    public string Pais { get; set; } = string.Empty;
    /// <summary>Dirección del sitio web de la marca.</summary>
    public string Web { get; set; } = string.Empty;
    /// <summary>Nombres de los modelos disponibles de la marca.</summary>
    public ICollection<string> ModelosDisponibles { get; set; } = [];
};
