using Alquiler_Vehiculos.Models.Email;

namespace Alquiler_Vehiculos.Dto.Email;

/// <summary>
///     RF-34: aviso de que se devuelve un importe al cliente, ya sea por
///     anulación de la reserva (CU-09) o por la penalización de un no
///     presentado (CU-13).
/// </summary>
public sealed record ReembolsoEmail : NotificacionEmailBase {
    /// <summary>Motivo del reembolso.</summary>
    public required TipoReembolso Motivo { get; init; }

    /// <summary>Importe que se devuelve al cliente.</summary>
    public required decimal ImporteReembolsado { get; init; }

    /// <summary>Importe retenido (en un no presentado, un día de alquiler).</summary>
    public decimal ImporteRetenido { get; init; }

    /// <summary>Marca de la tarjeta a la que se devuelve el dinero.</summary>
    public required string MarcaTarjeta { get; init; }

    /// <summary>Cuatro últimos dígitos de la tarjeta.</summary>
    public required string UltimosCuatroDigitos { get; init; }

    /// <summary>Plazo estimado de abono por el banco.</summary>
    public string PlazoEstimado { get; init; } = "Entre 3 y 7 días laborables, según tu banco.";

    /// <summary>Motivo en texto para la plantilla.</summary>
    public string MotivoTexto => Motivo switch {
        TipoReembolso.AnulacionReserva => "Anulación de la reserva",
        TipoReembolso.PenalizacionNoPresentado => "Penalización por no presentarse a la recogida",
        _ => "Reembolso"
    };

    /// <inheritdoc />
    public override string Asunto => $"Reembolso de la reserva {CodigoReferencia}";
}
