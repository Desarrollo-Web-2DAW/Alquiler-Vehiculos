namespace Alquiler_Vehiculos.Dto.Email;

/// <summary>
///     RF-34: instrucciones de recogida (horario de la oficina y datos del vehículo).
/// </summary>
public sealed record ReservaRecogidaEmail : NotificacionEmailBase {
    /// <summary>Fecha y hora de recogida.</summary>
    public required DateTimeOffset FechaRecogida { get; init; }

    /// <summary>Fecha de devolución.</summary>
    public required DateOnly FechaDevolucion { get; init; }

    /// <summary>Vehículo asignado (marca, modelo y matrícula).</summary>
    public required string VehiculoDescripcion { get; init; }

    /// <summary>Matrícula del vehículo.</summary>
    public required string Matricula { get; init; }

    /// <summary>Color del vehículo.</summary>
    public required string Color { get; init; }

    /// <summary>Instrucciones concretas de recogida.</summary>
    public string Instrucciones { get; init; } =
        "Acude a la oficina dentro del horario de atención con tu documento de identidad y el permiso de conducir.";

    /// <inheritdoc />
    public override string Asunto => $"Tu reserva {CodigoReferencia}: instrucciones de recogida";
}
