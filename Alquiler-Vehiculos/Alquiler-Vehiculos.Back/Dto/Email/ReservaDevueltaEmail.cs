namespace Alquiler_Vehiculos.Dto.Email;

/// <summary>
///     RF-34: resumen del alquiler y coste final tras la devolución.
/// </summary>
public sealed record ReservaDevueltaEmail : NotificacionEmailBase {
    /// <summary>Fecha y hora de la devolución.</summary>
    public required DateTimeOffset FechaDevolucion { get; init; }

    /// <summary>Días facturados del alquiler (incluye días de retraso si los hubo, CU-24).</summary>
    public required int Dias { get; init; }

    /// <summary>Vehículo devuelto (marca, modelo y matrícula).</summary>
    public required string VehiculoDescripcion { get; init; }

    /// <summary>Líneas finales facturadas.</summary>
    public required IReadOnlyList<LineaEmailDto> Lineas { get; init; }

    /// <summary>Coste final del alquiler.</summary>
    public required decimal PrecioTotal { get; init; }

    /// <summary>Mensaje sobre la valoración del vehículo (RF-29).</summary>
    public string MensajeValoracion { get; init; } =
        "Ya puedes valorar el vehículo que has conducido desde tu historial de reservas.";

    /// <inheritdoc />
    public override string Asunto => $"Resumen de tu alquiler {CodigoReferencia}";
}
