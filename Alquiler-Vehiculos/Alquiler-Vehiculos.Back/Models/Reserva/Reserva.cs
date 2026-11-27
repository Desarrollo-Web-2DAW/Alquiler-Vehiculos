using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Enums;
using Alquiler_Vehiculos.Models.Pagos;
using Alquiler_Vehiculos.Models.Usuarios.Clientes;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;

namespace Alquiler_Vehiculos.Models.Reservas;

public class Reserva {
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Codigo { get; set; } = string.Empty;
    
    public Guid ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public string VehiculoId { get; set; } = string.Empty;
    public Guid? TrabajadorId { get; set; }
    public Trabajador? Trabajador { get; set; }

    public DateTime FechaRecogida { get; set; }
    public DateTime FechaDevolucion { get; set; }
    public DateTime? FechaDevolucionReal { get; set; }
    public DateTime? ExpiraEn { get; set; }

    public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;
    
    public decimal PrecioBaseDiaCongelado { get; set; }
    public string NombrePolizaCongelada { get; set; } = string.Empty;
    public decimal IncrementoPolizaDiaCongelado { get; set; }
    public decimal PrecioTotal { get; set; }
    public string Divisa { get; set; } = ReservasConstantes.DivisaPorDefecto;
    
    public string? Observaciones { get; set; }
    public string? MotivoCancelacion { get; set; }
    
    public uint Version { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    public ICollection<LineaReserva> Lineas { get; set; } = new List<LineaReserva>();
    public ICollection<CambioEstadoReserva> HistorialEstados { get; set; } = new List<CambioEstadoReserva>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}