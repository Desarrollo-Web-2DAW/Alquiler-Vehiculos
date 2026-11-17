namespace Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Modelo;

/// <summary>
/// Datos de entrada para actualizar un modelo de vehículo.
/// </summary>
public record UpdateRequestModeloDto {
    /// <summary>Nombre del modelo.</summary>
    public string Nombre { get; set; } = string.Empty;
    /// <summary>Año del modelo.</summary>
    public int Anio { get; set; }
    /// <summary>Nombre de la marca a la que pertenece el modelo.</summary>
    public string NombreMarca { get; set; } = string.Empty;
};
