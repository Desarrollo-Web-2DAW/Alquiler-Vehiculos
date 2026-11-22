using Alquiler_Vehiculos.Enums.Taller;

namespace Alquiler_Vehiculos.Models.Taller;

/// <summary>
/// Parte de taller: cada entrada de un vehículo al taller abre un parte (RF-08).
/// Es un documento interno del taller: los clientes no lo ven nunca (RF-46).
/// </summary>
public class ParteTaller {
    /// <summary>Identificador del parte.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Fecha y hora de entrada al taller. Es la que consulta el panel de
    /// administración junto al vehículo (CU-08).
    /// </summary>
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Trabajador que tramita el parte: guarda su <c>Trabajador.Id</c> (RF-08, RF-12).
    /// Solo un trabajador de tipo <c>Mecanico</c> puede abrir y cerrar partes.
    /// </summary>
    public Guid QuienLoTramita { get; set; }

    /// <summary>Descripción del trabajo que se hace en el taller.</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Vehículo que entra al taller: guarda su identificador corto de 11 caracteres (RF-01).
    /// La clave foránea al modelo de vehículo se cablea cuando se fusione ese módulo.
    /// </summary>
    public string VehiculoId { get; set; } = string.Empty;

    /// <summary>
    /// Estado del parte (RF-08). Mientras esté <see cref="EstadoParte.Abierto"/> el
    /// vehículo se mantiene en <c>Mantenimiento</c>; solo con el parte
    /// <see cref="EstadoParte.Cerrado"/> puede volver a <c>Disponible</c>.
    /// </summary>
    public EstadoParte Estado { get; set; } = EstadoParte.Abierto;
}
