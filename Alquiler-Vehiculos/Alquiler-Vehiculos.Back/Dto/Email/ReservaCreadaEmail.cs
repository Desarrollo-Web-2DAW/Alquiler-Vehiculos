namespace Alquiler_Vehiculos.Dto.Email;

/// <summary>
///     RF-34: acuse de alta de reserva con detalle (vehículo o categoría,
///     fechas, líneas y precio).
/// </summary>
public sealed record ReservaCreadaEmail : NotificacionEmailBase {
    /// <summary>Categoría pedida por el cliente (RF-27).</summary>
    public required string CategoriaPedida { get; init; }

    /// <summary>Vehículo concreto asignado (marca, modelo y matrícula).</summary>
    public required string VehiculoDescripcion { get; init; }

    /// <summary>Matrícula del vehículo asignado.</summary>
    public required string Matricula { get; init; }

    /// <summary>Fecha de recogida.</summary>
    public required DateOnly FechaRecogida { get; init; }

    /// <summary>Fecha de devolución.</summary>
    public required DateOnly FechaDevolucion { get; init; }

    /// <summary>Días de alquiler (fecha a fecha, RF-19).</summary>
    public required int Dias { get; init; }

    /// <summary>Líneas de la reserva (vehículo, póliza, extras y recargo).</summary>
    public required IReadOnlyList<LineaEmailDto> Lineas { get; init; }

    /// <summary>Precio total de la reserva.</summary>
    public required decimal PrecioTotal { get; init; }

    /// <inheritdoc />
    public override string Asunto => $"Tu reserva {CodigoReferencia} está creada";
}
