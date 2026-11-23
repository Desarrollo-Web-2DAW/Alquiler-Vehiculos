namespace Alquiler_Vehiculos.Dto.Email;

/// <summary>
///     RF-34: acuse de pago aprobado con el importe, los cuatro últimos
///     dígitos de la tarjeta y cómo recoger el vehículo.
/// </summary>
public sealed record PagoAprobadoEmail : NotificacionEmailBase {
    /// <summary>Importe cobrado.</summary>
    public required decimal Importe { get; init; }

    /// <summary>Marca de la tarjeta (Visa, Mastercard...).</summary>
    public required string MarcaTarjeta { get; init; }

    /// <summary>Cuatro últimos dígitos de la tarjeta (el número completo nunca viaja).</summary>
    public required string UltimosCuatroDigitos { get; init; }

    /// <summary>Fecha y hora del pago.</summary>
    public required DateTimeOffset FechaPago { get; init; }

    /// <summary>Fecha y hora de recogida acordada.</summary>
    public required DateTimeOffset FechaRecogida { get; init; }

    /// <summary>Vehículo asignado (marca, modelo y matrícula).</summary>
    public required string VehiculoDescripcion { get; init; }

    /// <summary>Instrucciones de recogida.</summary>
    public string InstruccionesRecogida { get; init; } =
        "Preséntate en la oficina con tu documento de identidad y la tarjeta usada en el pago.";

    /// <inheritdoc />
    public override string Asunto => $"Pago de la reserva {CodigoReferencia} aprobado";
}
