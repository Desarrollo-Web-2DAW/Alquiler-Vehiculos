namespace Alquiler_Vehiculos.Models.Trazabilidad;

public class Valoracion {
    public int Id { get; init; }
    public int Puntuacion { get; set; }
    public string Texto { get; set; } = string.Empty;
    public DateTimeOffset Fecha { get; init; }
    
    public Reserva.Reserva Reserva { get; init; } = null!;
    public int ReservaId { get; init; }
    
    public Cliente Cliente { get; init; } = null!;
    public Guid ClienteId { get; init; }
    
    public Vehiculo.Vehiculo Vehiculo { get; init; } = null!;
    public string VehiculoId { get; init; } = string.Empty;
    
    public DateTimeOffset CreatedAt { get; init; } 
    public DateTimeOffset UpdatedAt { get; set; } 
    public bool IsDeleted { get; set; }
}