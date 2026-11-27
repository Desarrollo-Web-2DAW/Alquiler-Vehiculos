using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Enums;
using ReservaEntity = Alquiler_Vehiculos.Models.Reserva.Reserva;
namespace Alquiler_Vehiculos.Models.Pago;

public class Pago {
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ReservaId { get; set; }
    public ReservaEntity Reserva { get; set; } = null!;
    
    public decimal Importe { get; set; }
    public decimal? ImporteReembolsado { get; set; }
    public string Divisa { get; set; } = ReservasConstantes.DivisaPorDefecto;
    
    public EstadoPago Estado { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public string MarcaTarjeta { get; set; } = string.Empty;
    public string UltimosCuatroDigitos { get; set; } = null!;

    public string NumeroTarjeta { set; get; } = string.Empty;

    public string ReferenciaPasarela { get; set; } = string.Empty;
    
    public string? MotivoRechazo { get; set; }
}