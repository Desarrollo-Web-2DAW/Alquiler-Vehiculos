namespace Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Modelo;

/// <summary>
/// Datos de un modelo de vehículo y su marca devueltos por la API.
/// </summary>
public record ModeloDto {
    /// <summary>Nombre del modelo.</summary>
    public string Nombre { get; set; } = string.Empty;
    /// <summary>Año del modelo.</summary>
    public int Anio { get; set; }
    /// <summary>Nombre de la marca a la que pertenece el modelo.</summary>
    public string NombreMarca { get; set; } = string.Empty;
};
