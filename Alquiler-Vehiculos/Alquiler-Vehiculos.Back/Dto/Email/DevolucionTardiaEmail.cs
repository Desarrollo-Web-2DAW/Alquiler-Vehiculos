namespace Alquiler_Vehiculos.Dto.Email;

/// <summary>
///     CU-24 (acuerdo de equipo): aviso al cliente afectado por una
///     devolución tardía del vehículo que tenía reservado.
/// </summary>
public sealed record DevolucionTardiaEmail : NotificacionEmailBase {
    /// <summary>Detalle de la demora para el cliente.</summary>
    public required string Detalle { get; init; }

    /// <summary>Nueva fecha y hora estimada de recogida.</summary>
    public required string NuevaFechaRecogidaEstimada { get; init; }

    /// <summary>Alternativas ofrecidas (esperar, otro vehículo o cancelar sin coste).</summary>
    public required string Alternativas { get; init; }

    /// <summary>Vehículo afectado (marca, modelo y matrícula).</summary>
    public required string VehiculoDescripcion { get; init; }

    /// <inheritdoc />
    public override string Asunto => $"Cambio en la recogida de tu reserva {CodigoReferencia}";
}
