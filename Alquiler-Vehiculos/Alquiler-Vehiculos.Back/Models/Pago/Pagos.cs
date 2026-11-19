using Alquiler_Vehiculos.Models.Pago.EstadoPagos;

namespace Alquiler_Vehiculos.Models.Pago;

public class Pagos {
    public long Id {get; init;}
    public decimal Importe { get; init; }
    public string Divisa { get; init; }
    public DateTimeOffset Fecha { get; init; } = DateTime.UtcNow;
    public EstadoPago Estado { get; set; }
    public string MarcaTarjeta { get; init; } = string.Empty;
    public string UltimosCuatroDigitos { get; init; } = string.Empty;
    public string NumeroTarjetaCifrado { get; init; } = string.Empty;
    public string ReferenciaPasarela { get; set; } = string.Empty;
    
    public Reservas.Reserva Reserva { get; init; } = null!;
    public int ReservaId { get; init; }
    
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}