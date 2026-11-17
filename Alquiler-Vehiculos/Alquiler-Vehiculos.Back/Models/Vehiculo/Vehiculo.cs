using Alquiler_Vehiculos.Models.Vehiculo.Enums;

namespace Alquiler_Vehiculos.Models.Vehiculo;

public class Vehiculo
{
    public string Id { get; init; } = null!;
    public string FotoFk { get; init; } = string.Empty;
    public string  Matrícula { get; init; } = string.Empty;
    public string Marca { get; init; } = string.Empty;
    public string Modelo { get; init; } = string.Empty;
    public string Acriss { get; init; } = string.Empty;
    public TipoCarroceria Carroceria { get; init; }
    public TipoCombustible Combustible { get; init; }
    public TipoTransmision Transmision { get; init; }
    public EstadoVehiculo Estado { get; init; } = EstadoVehiculo.Disponible;
    public TipoColor Color { get; init; }
    public double PrecioBaseDia { get; init; }
    public int Plazas { get; init; }
    public int MaleteroLitros { get; init; }
    public int AutonomiaKm { get; init; }
    public int Kilometraje { get; init; }
    public DateTime FechaUltimaRevision { get; init; }
    public long ConesionarioFk { get; init; }
    public bool Activo { get; init; } = true;
}