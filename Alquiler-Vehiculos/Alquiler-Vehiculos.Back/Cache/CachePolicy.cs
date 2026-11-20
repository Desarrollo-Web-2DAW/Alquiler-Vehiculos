namespace Alquiler_Vehiculos.Cache;

/// <summary>
/// Política pura de la caché: los cálculos del TTL mixto
/// (ventana móvil + techo de vida máxima) y de expulsión LRU.
/// No tiene dependencias para poder probarse de forma unitaria (RNF-02, RNF-04).
/// </summary>
public static class CachePolicy {
    /// <summary>
    /// Deadline absoluto de una entrada: nace con el y nunca se supera,
    /// aunque la ventana móvil renueve la entrada en cada acceso.
    /// </summary>
    /// <param name="now">Momento actual.</param>
    /// <param name="absoluteMaxLifetime">Vida máxima de la entrada.</param>
    /// <returns>Fecha límite de la entrada.</returns>
    public static DateTimeOffset ComputeDeadline(DateTimeOffset now, TimeSpan absoluteMaxLifetime) {
        return now + absoluteMaxLifetime;
    }

    /// <summary>
    /// TTL efectivo de una entrada: la ventana móvil, recortada si lo que queda
    /// de vida máxima es menos que ella. Así la renovación nunca rebasa el deadline.
    /// </summary>
    /// <param name="now">Momento actual.</param>
    /// <param name="deadline">Deadline absoluto de la entrada.</param>
    /// <param name="sliding">Ventana móvil.</param>
    /// <returns>TTL que se aplica ahora mismo a la entrada.</returns>
    public static TimeSpan EffectiveTtl(DateTimeOffset now, DateTimeOffset deadline, TimeSpan sliding) {
        var remaining = deadline - now;
        if (remaining <= TimeSpan.Zero) {
            return TimeSpan.Zero;
        }
        return remaining < sliding ? remaining : sliding;
    }

    /// <summary>
    /// Indica si una entrada ha agotado su vida máxima.
    /// </summary>
    /// <param name="now">Momento actual.</param>
    /// <param name="deadline">Deadline absoluto de la entrada.</param>
    /// <returns>true si la entrada está caducada por su techo de vida máxima.</returns>
    public static bool IsExpired(DateTimeOffset now, DateTimeOffset deadline) {
        return now >= deadline;
    }

    /// <summary>
    /// Cuántas entradas hay que expulsar por LRU para respetar la capacidad,
    /// contando ya la entrada que se acaba de guardar.
    /// </summary>
    /// <param name="currentCount">Entradas actuales en la caché (incluida la nueva).</param>
    /// <param name="capacity">Capacidad máxima.</param>
    /// <returns>Número de entradas a expulsar (0 si no hay que expulsar).</returns>
    public static int EvictionCount(int currentCount, int capacity) {
        return currentCount > capacity ? currentCount - capacity : 0;
    }
}
