namespace Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Query;

/// <summary>
/// Filtros opcionales para consultar el catálogo de vehículos (RF-06).
/// </summary>
/// <remarks>
/// Los filtros proporcionados se combinan: el vehículo debe cumplir todos ellos.
/// Un valor nulo indica que no se aplica ese filtro.
/// Las fechas de recogida y devolución deben proporcionarse juntas para consultar un rango.
/// El perfil del usuario no forma parte de los filtros: se obtiene de la autenticación.
/// </remarks>
public record QueryRequestVehiculoDto {
    
    /// <summary>
    /// Texto que debe contener la marca, sin distinguir mayúsculas y minúsculas.
    /// </summary>
    public string? Marca { get; set; }

    /// <summary>
    /// Texto que debe contener el modelo, sin distinguir mayúsculas y minúsculas.
    /// </summary>
    public string? Modelo { get; set; }

    /// <summary>
    /// Categoría de negocio solicitada, correspondiente a un valor de TipoCarroceria.
    /// </summary>
    public string? Categoria { get; set; }

    /// <summary>
    /// Estado actual solicitado, correspondiente a un valor de EstadoVehiculo.
    /// </summary>
    public string? Estado { get; set; }

    /// <summary>
    /// Tipo de combustible solicitado: Gasolina, Diesel, Hibrido o Electrico.
    /// </summary>
    public string? Combustible { get; set; }

    /// <summary>
    /// Transmisión solicitada: Manual o Automatica.
    /// </summary>
    public string? Transmision { get; set; }

    /// <summary>
    /// Número mínimo de plazas que debe tener el vehículo.
    /// </summary>
    public int? PlazasMinimas { get; set; }

    /// <summary>
    /// Capacidad mínima del maletero, expresada en litros.
    /// </summary>
    public int? MaleteroLitrosMinimos { get; set; }

    /// <summary>
    /// Autonomía mínima del vehículo, expresada en kilómetros.
    /// </summary>
    public int? AutonomiaKmMinima { get; set; }

    /// <summary>
    /// Fecha y hora de recogida del rango para el que se consulta disponibilidad.
    /// </summary>
    public DateTime? FechaRecogida { get; set; }

    /// <summary>
    /// Fecha y hora de devolución del rango para el que se consulta disponibilidad.
    /// </summary>
    public DateTime? FechaDevolucion { get; set; }
}