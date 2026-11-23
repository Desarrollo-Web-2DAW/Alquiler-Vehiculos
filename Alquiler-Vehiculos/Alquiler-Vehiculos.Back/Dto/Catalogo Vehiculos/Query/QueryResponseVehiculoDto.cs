namespace Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Query;

/// <summary>
/// Recuento de vehículos de una misma marca, modelo y categoría que cumplen los filtros (RF-06).
/// </summary>
/// <remarks>
/// Representa un grupo, no una unidad concreta. La búsqueda devuelve una colección de estos grupos.
/// El administrador puede consultar grupos sin unidades libres; en la respuesta del cliente se omiten.
/// </remarks>
public record QueryResponseVehiculoDto {
    
    /// <summary>
    /// Marca que identifica el grupo de vehículos.
    /// </summary>
    public string Marca { get; set; } = string.Empty;
    
    /// <summary>
    /// Modelo que identifica el grupo de vehículos.
    /// </summary>
    public string Modelo { get; set; } = string.Empty;
    
    /// <summary>
    /// Categoría de negocio del grupo de vehículos.
    /// </summary>
    public string Categoria { get; set; } = string.Empty;
    
    /// <summary>
    /// Número total de unidades del grupo que cumplen los filtros, incluidas las ocupadas.
    /// </summary>
    public int UnidadesTotales { get; set; }
    
    /// <summary>
    /// Número de unidades del grupo libres para el rango solicitado o para el momento actual si no se indican fechas.
    /// </summary>
    public int UnidadesLibres { get; set; }
}