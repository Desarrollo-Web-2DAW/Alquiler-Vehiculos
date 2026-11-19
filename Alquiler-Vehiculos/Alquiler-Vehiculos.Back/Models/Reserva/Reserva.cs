using Alquiler_Vehiculos.Models.Reserva.Enum;

namespace Alquiler_Vehiculos.Models.Reserva;

public class Reserva {
    public int Id {get; init;}
    public string CodigoReferencia {get; init;} = string.Empty;
    public string CategoriaSolicitada { get; init; } = string.Empty;
    public DateTime FechaCreacion { get; init; }
    public DateTime FechaRecogida  { get; init; }
    public DateTime FechaDevolucion   { get; init; }
    public EstadoReserva Estado { get; init; }
    public decimal PrecioTotal { get; init; }
    public string Notas { get; init; } = string.Empty;

    public Cliente Cliente { get; init; } = null!;
    public Guid ClienteId {get; init;}
    
    public User CreadoPorUsuario { get; init; }
    public Guid CreadoPorUsuarioId  { get; init; }

    public Vehiculo.Vehiculo Vehiculo { get; init; } = null!;
    public string VehiculoId { get; init; } = string.Empty;
    
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public bool IsDeleted {get; init;}
}