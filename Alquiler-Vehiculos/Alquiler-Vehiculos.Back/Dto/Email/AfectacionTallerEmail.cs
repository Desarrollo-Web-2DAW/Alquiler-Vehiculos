using Alquiler_Vehiculos.Models.Email;

namespace Alquiler_Vehiculos.Dto.Email;

/// <summary>
///     RF-34 / RF-07: aviso al cliente de qué pasa con su reserva cuando el
///     vehículo entra al taller (cambio de vehículo, cancelación o aplazamiento).
/// </summary>
public sealed record AfectacionTallerEmail : NotificacionEmailBase {
    /// <summary>Solución aplicada a la reserva.</summary>
    public required TipoAfectacionTaller TipoAfectacion { get; init; }

    /// <summary>Detalle de la solución para el cliente.</summary>
    public required string Detalle { get; init; }

    /// <summary>Vehículo original de la reserva (marca, modelo y matrícula).</summary>
    public required string VehiculoDescripcion { get; init; }

    /// <summary>Nuevo vehículo asignado, si hay cambio de vehículo.</summary>
    public string NuevoVehiculoDescripcion { get; init; } = string.Empty;

    /// <summary>Nueva fecha de recogida, si hay aplazamiento.</summary>
    public string NuevaFechaRecogida { get; init; } = string.Empty;

    /// <summary>Solución en texto para la plantilla.</summary>
    public string TipoAfectacionTexto => TipoAfectacion switch {
        TipoAfectacionTaller.CambioVehiculo => "Cambio de vehículo",
        TipoAfectacionTaller.CancelacionReserva => "Cancelación de la reserva",
        TipoAfectacionTaller.Aplazamiento => "Aplazamiento de la recogida",
        _ => "Cambio en tu reserva"
    };

    /// <inheritdoc />
    public override string Asunto => $"Cambios en tu reserva {CodigoReferencia} por una revisión del vehículo";
}
