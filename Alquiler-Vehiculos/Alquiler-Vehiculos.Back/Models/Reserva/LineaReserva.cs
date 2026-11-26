using Alquiler_Vehiculos.Enums;

namespace Alquiler_Vehiculos.Models.Reserva;

public class LineaReserva {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReservaId { get; set; }
    public Reserva Reserva { get; set; } 
    
    public TipoLineaReserva Tipo { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    
    public Guid? PolizaId { get; set; }
    public Guid? ExtraId { get; set; }
}