namespace Alquiler_Vehiculos.Dto.Email;

/// <summary>
///     RF-34: acuse de cancelación con el detalle de la reserva.
/// </summary>
public sealed record ReservaCanceladaEmail : NotificacionEmailBase {
    /// <summary>Fecha y hora de la cancelación.</summary>
    public required DateTimeOffset FechaCancelacion { get; init; }

    /// <summary>Motivo de la cancelación.</summary>
    public string Motivo { get; init; } = "Cancelación solicitada por el cliente.";

    /// <summary>Fecha de recogida que quedó sin efecto.</summary>
    public required DateOnly FechaRecogida { get; init; }

    /// <summary>Fecha de devolución que quedó sin efecto.</summary>
    public required DateOnly FechaDevolucion { get; init; }

    /// <summary>Líneas de la reserva cancelada.</summary>
    public required IReadOnlyList<LineaEmailDto> Lineas { get; init; }

    /// <summary>Precio total de la reserva cancelada.</summary>
    public required decimal PrecioTotal { get; init; }

    /// <inheritdoc />
    public override string Asunto => $"Cancelación de la reserva {CodigoReferencia}";
}
