namespace Alquiler_Vehiculos.Dto.Email;

/// <summary>
///     RF-34 / RF-38: aviso de que la reserva ha caducado por falta de pago.
/// </summary>
public sealed record ReservaCaducadaEmail : NotificacionEmailBase {
    /// <summary>Motivo de la caducidad.</summary>
    public string Motivo { get; init; } = "No se recibió el pago en los 30 minutos siguientes al alta de la reserva.";

    /// <summary>Vehículo que quedaba retenido (marca, modelo y matrícula).</summary>
    public required string VehiculoDescripcion { get; init; }

    /// <summary>Fecha de recogida que quedó sin efecto.</summary>
    public required DateOnly FechaRecogida { get; init; }

    /// <summary>Fecha de devolución que quedó sin efecto.</summary>
    public required DateOnly FechaDevolucion { get; init; }

    /// <inheritdoc />
    public override string Asunto => $"Tu reserva {CodigoReferencia} ha caducado";
}
