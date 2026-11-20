using Alquiler_Vehiculos.Models.Reserva.Enum;

namespace Alquiler_Vehiculos.Models.Trazabilidad;

public class CambioEstadoReserva {
    public int Id { get; init; }
    public EstadoReserva? EstadoAnterior { get; init; }
    public EstadoReserva EstadoNuevo { get; init; }
    public DateTimeOffset Fecha { get; init; }
    public string Motivo { get; init; } = string.Empty;
    public Reserva.Reserva Reserva { get; init; } = null!;
    public int ReservaId { get; init; }
    public User? Usuario { get; init; }
    public Guid? UsuarioId { get; init; }
}