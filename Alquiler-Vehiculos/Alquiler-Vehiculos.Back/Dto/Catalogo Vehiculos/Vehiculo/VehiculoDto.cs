
namespace Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;

public record VehiculoDto {
    
    public string Matricula { get; set; } = string.Empty;
    public string Acriss { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal PrecioBaseDia { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Combustible { get; set; } = string.Empty;
    public string Transmision { get; set; } = string.Empty;
    public int Plazas { get; set; }
    public int MaleteroLitros { get; set; }
    public int AutonomiaKm { get; set; }
    public int Kilometraje { get; set; }
    public string FechaUltimaRevision { get; set; } = string.Empty;
    public int ConcesionarioId { get; set; }
    public string Marca { get; set; } = string.Empty; 
    public string Modelo { get; set; } = string.Empty;
}