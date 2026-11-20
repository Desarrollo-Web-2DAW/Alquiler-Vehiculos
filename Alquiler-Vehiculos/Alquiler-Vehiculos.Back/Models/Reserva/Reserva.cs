using Alquiler_Vehiculos.Models.Reserva.Enum;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;

namespace Alquiler_Vehiculos.Models.Reserva;

public class Reserva {
    public int Id {get; init;}
    public string CodigoReferencia {get; init;} = string.Empty;
    public TipoCarroceria CategoriaSolicitada { get; init; }
    public DateTimeOffset FechaCreacion { get; init; }
    public DateTimeOffset FechaRecogida  { get; set; }
    public DateTimeOffset FechaDevolucion   { get; set; }
    public EstadoReserva Estado { get; set; }
    public decimal PrecioTotal { get; set; }
    public string Notas { get; set; } = string.Empty;

    public Cliente Cliente { get; init; } = null!;
    public Guid ClienteId {get; init;}

    public User CreadoPorUsuario { get; set; } = null!;
    public Guid CreadoPorUsuarioId  { get; set; }

    public Vehiculo.Vehiculo Vehiculo { get; set; } = null!;
    public string VehiculoId { get; set; } = string.Empty;
    
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted {get; set;}
}