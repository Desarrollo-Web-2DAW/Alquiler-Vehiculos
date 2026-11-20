using Alquiler_Vehiculos.Models.Pago.EstadoPagos;

namespace Alquiler_Vehiculos.Models.Pago;

public class Pagos {
    public long Id {get; init;}
    public decimal Importe { get; set; }
    public string Divisa { get; init; }  = string.Empty;
    public DateTimeOffset Fecha { get; init; }
    public EstadoPago Estado { get; set; }
    public string MarcaTarjeta { get; init; } = string.Empty;
    public string UltimosCuatroDigitos { get; init; } = string.Empty;
    public string NumeroTarjetaCifrado { get; init; } = string.Empty;
    public string ReferenciaPasarela { get; set; } = string.Empty;
    
    public Reserva.Reserva Reserva { get; init; } = null!;
    public int ReservaId { get; init; }
    
    public DateTimeOffset CreatedAt { get; init; } 
    public DateTimeOffset UpdatedAt { get; set; } 
    public bool IsDeleted { get; set; }
}