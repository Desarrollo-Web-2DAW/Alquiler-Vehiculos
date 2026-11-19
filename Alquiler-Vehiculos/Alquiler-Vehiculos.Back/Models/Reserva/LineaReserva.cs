using Alquiler_Vehiculos.Models.Reserva.Enum;

namespace Alquiler_Vehiculos.Models.Reserva;

public class LineaReserva {
    public int Id { get; init; }
    public TipoLineaReserva TipoLineaReserva { get; init; }
    public string Concepto { get; init; } = string.Empty;
    public int Cantidad { get; init;}
    public decimal PrecioUnitario { get; init;}
    public decimal SubTotal { get; init;}

    public Reservas.Reserva Reserva { get; init; } = null!;
    public int ReservaId {get; init;}

    public Poliza? Poliza { get; init; } = null!;
    public int? PolizaId {get; init;}

    public Extra? Extra { get; init; } = null!;
    public int? ExtraId {get; init;}
    
    public DateTime CreatedAt { get; init;}
    public DateTime UpdatedAt { get; init;}
    public bool IsDeleted { get; init;}
}