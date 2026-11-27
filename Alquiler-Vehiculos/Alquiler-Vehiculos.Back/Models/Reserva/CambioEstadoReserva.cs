using Alquiler_Vehiculos.Enums;
using Alquiler_Vehiculos.Models.Usuarios;

namespace Alquiler_Vehiculos.Models.Reserva;

/// <summary>
/// Registro de auditoría de cada transición de la máquina de estados de una reserva (RF-31, RI-05).
/// Conserva quién hizo el cambio, cuándo y por qué.
/// </summary>
public class CambioEstadoReserva {
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ReservaId { get; set; }
    public Reserva Reserva { get; set; } = null!;
    
    public EstadoReserva? EstadoAnterior { get; set; }
    public EstadoReserva EstadoNuevo { get; set; }
    
    public Guid? RealizadoPorUsuarioId { get; set; }
    public User? RealizadoPorUsuario { get; set; }
    
    public string? Motivo { get; set; }
    public DateTime FechaCambio { get; set; } = DateTime.UtcNow;
}