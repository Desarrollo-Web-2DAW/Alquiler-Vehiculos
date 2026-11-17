using Alquiler_Vehiculos.Models.Vehiculo.Enums;

namespace Alquiler_Vehiculos.Models.Vehiculo;

public class Vehiculo
{
    public string Id { get; init; } = null!;
    public long ConcesionarioFk { get; init; }
    public long MarcaFk { get; init; }
    public long ModeloFk { get; init; }
    public TipoCarroceria Carroceria { get; init; }
}