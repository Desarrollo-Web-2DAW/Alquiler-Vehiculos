namespace Alquiler_Vehiculos.Cache;

/// <summary>
/// Opciones de configuración de la caché.
/// Se configuran desde appsettings.json en la sección "Cache".
/// </summary>
public class CacheOptions {
    /// <summary>
    /// Sección de configuración.
    /// </summary>
    public const string SectionName = "Cache";

    /// <summary>
    /// Capacidad máxima de la caché en número de entradas.
    /// Al superarla se expulsa la entrada menos usada recientemente (política LRU).
    /// </summary>
    public int Capacity { get; set; } = 500;

    /// <summary>
    /// Ventana móvil (segundos): si una entrada no se accede en este tiempo, caduca.
    /// Se renueva en cada acceso, pero sin rebasar la vida máxima (TTL mixto).
    /// </summary>
    public int SlidingExpirationSeconds { get; set; } = 60;

    /// <summary>
    /// Vida máxima absoluta (segundos): techo que ninguna entrada supera
    /// aunque se siga consultando. Es el deadline del TTL mixto.
    /// </summary>
    public int AbsoluteMaxLifetimeSeconds { get; set; } = 600;

    /// <summary>
    /// Periodo (segundos) de la tarea de poda del índice de recencia y etiquetas.
    /// </summary>
    public int PruneIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Convierte los segundos de la ventana móvil a TimeSpan.
    /// </summary>
    public TimeSpan SlidingExpiration => TimeSpan.FromSeconds(SlidingExpirationSeconds);

    /// <summary>
    /// Convierte los segundos de la vida máxima a TimeSpan.
    /// </summary>
    public TimeSpan AbsoluteMaxLifetime => TimeSpan.FromSeconds(AbsoluteMaxLifetimeSeconds);

    /// <summary>
    /// Convierte los segundos del periodo de poda a TimeSpan.
    /// </summary>
    public TimeSpan PruneInterval => TimeSpan.FromSeconds(PruneIntervalSeconds);
}
