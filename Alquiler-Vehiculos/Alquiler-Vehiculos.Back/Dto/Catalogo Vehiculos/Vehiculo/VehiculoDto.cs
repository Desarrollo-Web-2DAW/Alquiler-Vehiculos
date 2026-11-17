
namespace Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;

/// <summary>
/// Datos de un vehículo, su concesionario, marca y modelo devueltos por la API.
/// </summary>
public record VehiculoDto {
    
    /// <summary>Matrícula del vehículo.</summary>
    public string Matricula { get; set; } = string.Empty;
    /// <summary>Código ACRISS que clasifica el vehículo.</summary>
    public string Acriss { get; set; } = string.Empty;
    /// <summary>Categoría del vehículo.</summary>
    public string Categoria { get; set; } = string.Empty;
    /// <summary>Precio base de alquiler por día.</summary>
    public decimal PrecioBaseDia { get; set; }
    /// <summary>Color del vehículo.</summary>
    public string Color { get; set; } = string.Empty;
    /// <summary>Estado del vehículo.</summary>
    public string Combustible { get; set; } = string.Empty;
    /// <summary>Tipo de transmisión del vehículo.</summary>
    public string Transmision { get; set; } = string.Empty;
    /// <summary>Número de plazas del vehículo.</summary>
    public int Plazas { get; set; }
    /// <summary>Capacidad del maletero expresada en litros.</summary>
    public int MaleteroLitros { get; set; }
    /// <summary>Autonomía del vehículo expresada en kilómetros.</summary>
    public int AutonomiaKm { get; set; }
    /// <summary>Kilómetros recorridos por el vehículo.</summary>
    public string Marca { get; set; } = string.Empty; 
    /// <summary>Nombre del modelo del vehículo.</summary>
    public string Modelo { get; set; } = string.Empty;
}
