namespace Alquiler_Vehiculos.Dto.Email;

/// <summary>
///     CU-23 (acuerdo de equipo): aviso al cliente de que el vehículo
///     reservado no estará listo para su recogida, con alternativas.
/// </summary>
public sealed record VehiculoNoListoEmail : NotificacionEmailBase {
    /// <summary>Motivo por el que el vehículo no estará listo.</summary>
    public required string Motivo { get; init; }

    /// <summary>Detalle del motivo para el cliente.</summary>
    public required string Detalle { get; init; }

    /// <summary>Alternativas ofrecidas (otro vehículo, otra fecha o cancelación).</summary>
    public required string Alternativas { get; init; }

    /// <summary>Fecha y hora de recogida prevista.</summary>
    public required DateTimeOffset FechaRecogida { get; init; }

    /// <summary>Vehículo reservado (marca, modelo y matrícula).</summary>
    public required string VehiculoDescripcion { get; init; }

    /// <inheritdoc />
    public override string Asunto => $"Aviso sobre tu reserva {CodigoReferencia}: vehículo no disponible";
}
