using System.ComponentModel.DataAnnotations;
using Alquiler_Vehiculos.Enums.Taller;
using Alquiler_Vehiculos.Models.Taller;

namespace Alquiler_Vehiculos.Dto.Taller;

/// <summary>
/// Datos para abrir un parte de taller (RF-08). El trabajador que lo tramita
/// no viaja aquí: llega del contexto de quien llama (RF-12, RF-46).
/// </summary>
public record AbrirParteDto {
    /// <summary>Identificador corto de 11 caracteres del vehículo (RF-01).</summary>
    [Required, MaxLength(11)]
    public string VehiculoId { get; init; } = string.Empty;

    /// <summary>Trabajo que se va a hacer en el taller.</summary>
    [Required, MaxLength(1000)]
    public string Descripcion { get; init; } = string.Empty;
}

/// <summary>
/// Parte de taller tal como se expone al equipo interno (RF-08, RF-46).
/// Nunca se devuelve a los clientes.
/// </summary>
public record ParteTallerDto(
    Guid Id,
    DateTime Fecha,
    Guid QuienLoTramita,
    string VehiculoId,
    string Descripcion,
    EstadoParte Estado) {
    /// <summary>Crea el DTO desde el modelo del dominio.</summary>
    /// <param name="parte">Parte del dominio.</param>
    /// <returns>DTO con los datos del parte.</returns>
    public static ParteTallerDto Desde(ParteTaller parte) {
        return new ParteTallerDto(
            parte.Id,
            parte.Fecha,
            parte.QuienLoTramita,
            parte.VehiculoId,
            parte.Descripcion,
            parte.Estado);
    }
}

/// <summary>
/// Resultado de la comprobación de RF-01: si un vehículo tiene partes abiertos,
/// no se puede dar de baja hasta cerrarlos.
/// </summary>
public record TienePartesAbiertosDto(bool TienePartesAbiertos);
