using System.Text.Json.Serialization;

namespace Alquiler_Vehiculos.Enums.Taller;

/// <summary>
/// Estado del parte de taller (RF-08). La máquina de estados es deliberadamente
/// corta: un parte nace <see cref="Abierto"/> y solo pasa a <see cref="Cerrado"/>
/// cuando termina la reparación. No hay partes reabiertas: cada entrada al
/// taller abre un parte nuevo.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EstadoParte {
    /// <summary>El vehículo está en el taller; se mantiene en <c>Mantenimiento</c> (RF-05, RF-08).</summary>
    Abierto,

    /// <summary>La reparación ha terminado; el vehículo ya puede volver a <c>Disponible</c> (RF-08).</summary>
    Cerrado
}
