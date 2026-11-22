using Alquiler_Vehiculos.Models.Email;

namespace Alquiler_Vehiculos.Dto.Email;

/// <summary>
///     CU-12 (acuerdo de equipo): aviso al cliente cuando el vehículo sufre
///     una avería durante el alquiler y hay que resolver su reserva en curso.
/// </summary>
public sealed record AveriaDuranteAlquilerEmail : NotificacionEmailBase {
    /// <summary>Solución aplicada al alquiler en curso.</summary>
    public required SolucionAveria Solucion { get; init; }

    /// <summary>Detalle de lo ocurrido y de la solución.</summary>
    public required string Detalle { get; init; }

    /// <summary>Instrucciones para el cliente.</summary>
    public required string Instrucciones { get; init; }

    /// <summary>Vehículo afectado (marca, modelo y matrícula).</summary>
    public required string VehiculoDescripcion { get; init; }

    /// <summary>Matrícula del vehículo afectado.</summary>
    public required string Matricula { get; init; }

    /// <summary>Solución en texto para la plantilla.</summary>
    public string SolucionTexto => Solucion switch {
        SolucionAveria.FinAnticipado => "Fin anticipado del alquiler",
        SolucionAveria.VehiculoSustitucion => "Vehículo de sustitución",
        SolucionAveria.AplazamientoDevolucion => "Aplazamiento de la devolución",
        _ => "Solución en curso"
    };

    /// <inheritdoc />
    public override string Asunto => $"Aviso importante sobre tu alquiler {CodigoReferencia}";
}
