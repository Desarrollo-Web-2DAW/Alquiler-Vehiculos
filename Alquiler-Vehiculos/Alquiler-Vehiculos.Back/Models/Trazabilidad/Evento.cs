using Alquiler_Vehiculos.Models.Trazabilidad.Enums;

namespace Alquiler_Vehiculos.Models.Trazabilidad;

public class Evento {
    public int Id { get; init; }
    public TipoEvento Tipo { get; init; }
    public DateTimeOffset Fecha { get; init; }
    public decimal? Importe { get; init; }
    
    public Reserva.Reserva? Reserva { get; init; }
    public int? ReservaId { get; init; }
    
    public Vehiculo.Vehiculo Vehiculo { get; init; } = null!;
    public string VehiculoId { get; init; } = string.Empty;
}