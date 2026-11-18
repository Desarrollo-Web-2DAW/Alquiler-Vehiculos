using Alquiler_Vehiculos.Cache;

namespace Alquiler_Vehiculos.Tests.Unit.Cache;

/// <summary>
/// Tests unitarios de la política pura de la caché (TTL mixto y expulsión LRU).
/// Requisito cubierto: RNF-02.
/// </summary>
public class CachePolicyTests {
    private static readonly DateTimeOffset Ahora = new(2026, 11, 17, 10, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Dado que se crea una entrada, cuando se calcula su deadline,
    /// entonces es el momento actual más la vida máxima.
    /// </summary>
    [Test]
    public void ComputeDeadline_SumaLaVidaMaxima() {
        var deadline = CachePolicy.ComputeDeadline(Ahora, TimeSpan.FromMinutes(10));

        Assert.That(deadline, Is.EqualTo(Ahora.AddMinutes(10)));
    }

    /// <summary>
    /// Dado que a una entrada le queda más vida que la ventana móvil,
    /// cuando se calcula su TTL efectivo, entonces es la ventana móvil.
    /// </summary>
    [Test]
    public void EffectiveTtl_DevuelveLaVentanaMovilCuandoQuedaMasVida() {
        var deadline = Ahora.AddMinutes(10);

        var ttl = CachePolicy.EffectiveTtl(Ahora, deadline, TimeSpan.FromMinutes(1));

        Assert.That(ttl, Is.EqualTo(TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Dado que a una entrada le queda menos vida que la ventana móvil,
    /// cuando se calcula su TTL efectivo, entonces se recorta al deadline
    /// y la renovación nunca rebasa la vida máxima.
    /// </summary>
    [Test]
    public void EffectiveTtl_RecortaAlDeadlineSiQuedaMenosQueLaVentana() {
        var deadline = Ahora.AddSeconds(30);

        var ttl = CachePolicy.EffectiveTtl(Ahora, deadline, TimeSpan.FromMinutes(1));

        Assert.That(ttl, Is.EqualTo(TimeSpan.FromSeconds(30)));
    }

    /// <summary>
    /// Dado que una entrada ya superó su deadline,
    /// cuando se calcula su TTL efectivo, entonces es cero.
    /// </summary>
    [Test]
    public void EffectiveTtl_CeroSiYaPasoElDeadline() {
        var deadline = Ahora.AddSeconds(-1);

        var ttl = CachePolicy.EffectiveTtl(Ahora, deadline, TimeSpan.FromMinutes(1));

        Assert.That(ttl, Is.EqualTo(TimeSpan.Zero));
    }

    /// <summary>
    /// Dado que una entrada está viva, cuando se comprueba su caducidad,
    /// entonces no está caducada antes del deadline ni en el instante anterior.
    /// </summary>
    [Test]
    public void IsExpired_FalsoAntesDelDeadline() {
        var deadline = Ahora.AddMinutes(10);

        Assert.That(CachePolicy.IsExpired(Ahora, deadline), Is.False);
        Assert.That(CachePolicy.IsExpired(Ahora.AddSeconds(599), deadline), Is.False);
    }

    /// <summary>
    /// Dado que una entrada llega a su deadline, cuando se comprueba su caducidad,
    /// entonces está caducada justo en el deadline y después.
    /// </summary>
    [Test]
    public void IsExpired_CiertoDesdeElDeadline() {
        var deadline = Ahora.AddMinutes(10);

        Assert.That(CachePolicy.IsExpired(deadline, deadline), Is.True);
        Assert.That(CachePolicy.IsExpired(deadline.AddSeconds(1), deadline), Is.True);
    }

    /// <summary>
    /// Dado que la caché no supera su capacidad, cuando se calcula la expulsión,
    /// entonces no hay que expulsar nada.
    /// </summary>
    [Test]
    public void EvictionCount_CeroSiNoSeSuperaLaCapacidad() {
        Assert.That(CachePolicy.EvictionCount(3, capacity: 3), Is.EqualTo(0));
        Assert.That(CachePolicy.EvictionCount(2, capacity: 3), Is.EqualTo(0));
    }

    /// <summary>
    /// Dado que la caché supera su capacidad, cuando se calcula la expulsión,
    /// entonces hay que expulsar exactamente el exceso.
    /// </summary>
    [Test]
    public void EvictionCount_DevuelveElExceso() {
        Assert.That(CachePolicy.EvictionCount(4, capacity: 3), Is.EqualTo(1));
        Assert.That(CachePolicy.EvictionCount(6, capacity: 3), Is.EqualTo(3));
    }
}
