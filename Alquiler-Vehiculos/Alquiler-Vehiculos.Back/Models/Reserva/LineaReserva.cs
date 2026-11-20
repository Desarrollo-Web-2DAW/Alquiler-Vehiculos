using Alquiler_Vehiculos.Models.Reserva.Enum;

namespace Alquiler_Vehiculos.Models.Reserva;

public class LineaReserva {
    public int Id { get; init; }
    public TipoLineaReserva TipoLineaReserva { get; init; }
    public string Concepto { get; init; } = string.Empty;
    public int Cantidad { get; set;}
    public decimal PrecioUnitario { get; init;}
    public decimal SubTotal { get; set;}

    public Reserva Reserva { get; init; } = null!;
    public int ReservaId {get; init;}

    public Poliza? Poliza { get; init; } = null!;
    public int? PolizaId {get; init;}

    public Extra? Extra { get; init; } = null!;
    public int? ExtraId {get; init;}
    
    public DateTimeOffset CreatedAt { get; init;}
    public DateTimeOffset UpdatedAt { get; set;}
    public bool IsDeleted { get; set;}
}