namespace Alquiler_Vehiculos.Tests.Unit.Cache;

/// <summary>
/// Reloj falso para probar el TTL de la caché sin esperar tiempo real
/// (mismo criterio que las tareas programadas de RF-40).
/// </summary>
public class FakeTimeProvider(DateTimeOffset start) : TimeProvider {
    private DateTimeOffset _now = start;

    /// <summary>
    /// Avanza el reloj falso.
    /// </summary>
    /// <param name="delta">Tiempo a avanzar.</param>
    public void Advance(TimeSpan delta) => _now += delta;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => _now;
}
