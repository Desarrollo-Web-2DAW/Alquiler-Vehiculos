using Alquiler_Vehiculos.Cache;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Alquiler_Vehiculos.Tests.Unit.Cache;

/// <summary>
/// Tests unitarios de la caché LRU con TTL mixto (LruCacheService)
/// sobre un almacén falso, sin Redis. Requisito cubierto: RNF-02
/// (y la invalidación selectiva que después usará CU-15).
/// </summary>
public class LruCacheServiceTests {
    private static readonly DateTimeOffset Inicio = new(2026, 11, 17, 10, 0, 0, TimeSpan.Zero);

    private FakeTimeProvider _clock = null!;
    private FakeCacheStore _store = null!;

    [SetUp]
    public void Setup() {
        _clock = new FakeTimeProvider(Inicio);
        _store = new FakeCacheStore(_clock);
    }

    private LruCacheService CreateService(Action<CacheOptions>? configure = null) {
        var options = new CacheOptions {
            Capacity = 3,
            SlidingExpirationSeconds = 60,
            AbsoluteMaxLifetimeSeconds = 600,
            PruneIntervalSeconds = 60
        };
        configure?.Invoke(options);
        return new LruCacheService(_store, Options.Create(options), _clock, NullLogger<LruCacheService>.Instance);
    }

    /// <summary>
    /// Dado que la caché está vacía, cuando se consulta una clave,
    /// entonces devuelve default (fallo de caché).
    /// </summary>
    [Test]
    public async Task GetAsync_ClaveInexistente_DevuelveDefault() {
        var cache = CreateService();

        var result = await cache.GetAsync<string>("clave");

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Dado que se guarda un valor, cuando se consulta la misma clave,
    /// entonces devuelve el valor (acierto de caché).
    /// </summary>
    [Test]
    public async Task SetYGet_DevuelveElValor() {
        var cache = CreateService();

        await cache.SetAsync("clave", "valor");
        var result = await cache.GetAsync<string>("clave");

        Assert.That(result, Is.EqualTo("valor"));
    }

    /// <summary>
    /// Dado que una entrada no se vuelve a consultar, cuando pasa su ventana móvil,
    /// entonces caduca y ya no se devuelve.
    /// </summary>
    [Test]
    public async Task GetAsync_TrasLaVentanaMovilSinAccesos_Caduca() {
        var cache = CreateService();
        await cache.SetAsync("clave", "valor");

        _clock.Advance(TimeSpan.FromSeconds(61));
        var result = await cache.GetAsync<string>("clave");

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Dado que una entrada se consulta antes de caducar, cuando se consulta,
    /// entonces la ventana móvil se renueva y sobrevive más allá del plazo original.
    /// </summary>
    [Test]
    public async Task GetAsync_RenuevaLaVentanaMovilEnCadaAcceso() {
        var cache = CreateService();
        await cache.SetAsync("clave", "valor");

        _clock.Advance(TimeSpan.FromSeconds(50));
        Assert.That(await cache.GetAsync<string>("clave"), Is.EqualTo("valor"));

        // 100 s desde la escritura: solo sigue viva porque la lectura anterior la renovó
        _clock.Advance(TimeSpan.FromSeconds(50));
        Assert.That(await cache.GetAsync<string>("clave"), Is.EqualTo("valor"));

        _clock.Advance(TimeSpan.FromSeconds(61));
        Assert.That(await cache.GetAsync<string>("clave"), Is.Null);
    }

    /// <summary>
    /// Dado que una entrada se consulta de forma continua, cuando alcanza su vida máxima,
    /// entonces caduca igualmente: la renovación nunca rebasa el deadline (TTL mixto).
    /// </summary>
    [Test]
    public async Task GetAsync_NuncaSuperaLaVidaMaxima() {
        var cache = CreateService(); // vida máxima 600 s, ventana móvil 60 s
        await cache.SetAsync("clave", "valor");

        // lectura cada 50 s: siempre dentro de la ventana móvil, hasta llegar al deadline
        for (var i = 0; i < 11; i++) {
            _clock.Advance(TimeSpan.FromSeconds(50));
            Assert.That(await cache.GetAsync<string>("clave"), Is.EqualTo("valor"), $"lectura {i + 1} a los {50 * (i + 1)} s");
        }

        // 600 s desde la escritura: exactamente la vida máxima
        _clock.Advance(TimeSpan.FromSeconds(50));
        Assert.That(await cache.GetAsync<string>("clave"), Is.Null);
    }

    /// <summary>
    /// Dado que la caché supera su capacidad, cuando se guarda una entrada nueva,
    /// entonces se expulsa la menos usada recientemente (política LRU)
    /// y las más recientes siguen disponibles.
    /// </summary>
    [Test]
    public async Task Set_SuperaLaCapacidad_ExpulsaLaMenosUsada() {
        var cache = CreateService(); // capacidad 3

        await cache.SetAsync("a", "valor-a");
        _clock.Advance(TimeSpan.FromSeconds(1));
        await cache.SetAsync("b", "valor-b");
        _clock.Advance(TimeSpan.FromSeconds(1));
        await cache.SetAsync("c", "valor-c");
        _clock.Advance(TimeSpan.FromSeconds(1));

        // "a" vuelve a ser la más usada: "b" queda como la menos usada
        Assert.That(await cache.GetAsync<string>("a"), Is.EqualTo("valor-a"));
        _clock.Advance(TimeSpan.FromSeconds(1));

        await cache.SetAsync("d", "valor-d");

        Assert.That(await cache.ExistsAsync("a"), Is.True);
        Assert.That(await cache.ExistsAsync("b"), Is.False, "b es la menos usada y debe expulsarse");
        Assert.That(await cache.ExistsAsync("c"), Is.True);
        Assert.That(await cache.ExistsAsync("d"), Is.True);
    }

    /// <summary>
    /// Dado que varias entradas tienen etiquetas distintas, cuando se invalida una etiqueta,
    /// entonces solo se eliminan las entradas afectadas (invalidación selectiva, D4).
    /// </summary>
    [Test]
    public async Task RemoveByTag_SoloEliminaLasAfectadas() {
        var cache = CreateService();
        await cache.SetAsync("k1", "valor-1", tags: ["vehiculo:1"]);
        await cache.SetAsync("k2", "valor-2", tags: ["vehiculo:2"]);

        await cache.RemoveByTagAsync("vehiculo:1");

        Assert.That(await cache.ExistsAsync("k1"), Is.False);
        Assert.That(await cache.ExistsAsync("k2"), Is.True);
    }

    /// <summary>
    /// Dado que una clave se reescribe con otras etiquetas, cuando se invalida la etiqueta vieja,
    /// entonces la entrada sobrevive; al invalidar la nueva, se elimina.
    /// </summary>
    [Test]
    public async Task Set_ReescribirClave_ActualizaValorYEtiquetas() {
        var cache = CreateService();
        await cache.SetAsync("k", "valor-viejo", tags: ["t1"]);
        await cache.SetAsync("k", "valor-nuevo", tags: ["t2"]);

        await cache.RemoveByTagAsync("t1");
        Assert.That(await cache.GetAsync<string>("k"), Is.EqualTo("valor-nuevo"));

        await cache.RemoveByTagAsync("t2");
        Assert.That(await cache.ExistsAsync("k"), Is.False);
    }

    /// <summary>
    /// Dado que la caché tiene entradas, cuando se vacía,
    /// entonces no queda ninguna.
    /// </summary>
    [Test]
    public async Task Clear_VaciaLaCache() {
        var cache = CreateService();
        await cache.SetAsync("k1", "valor-1", tags: ["t1"]);
        await cache.SetAsync("k2", "valor-2", tags: ["t2"]);

        await cache.ClearAsync();

        Assert.That(await cache.ExistsAsync("k1"), Is.False);
        Assert.That(await cache.ExistsAsync("k2"), Is.False);
    }

    /// <summary>
    /// Dado que una clave no está en caché, cuando se usa GetOrSet,
    /// entonces se llama a la fábrica; en las consultas siguientes no vuelve a llamarse.
    /// </summary>
    [Test]
    public async Task GetOrSet_SoloLlamaALaFactoryCuandoFallaLaCache() {
        var cache = CreateService();
        var llamadas = 0;

        Task<string> Factory() {
            llamadas++;
            return Task.FromResult("calculado");
        }

        var primero = await cache.GetOrSetAsync("clave", Factory);
        var segundo = await cache.GetOrSetAsync("clave", Factory);

        Assert.That(primero, Is.EqualTo("calculado"));
        Assert.That(segundo, Is.EqualTo("calculado"));
        Assert.That(llamadas, Is.EqualTo(1));
    }

    /// <summary>
    /// Dado que hay entradas caducadas aún indexadas, cuando se poda a mano,
    /// entonces se limpian del índice y una segunda poda no encuentra nada
    /// (poda invocable sin esperar al timer, criterio de RF-40).
    /// </summary>
    [Test]
    public async Task Prune_LimpiaLasEntradasCaducadasDelIndice() {
        var cache = CreateService();
        await cache.SetAsync("k1", "valor-1");
        await cache.SetAsync("k2", "valor-2");

        _clock.Advance(TimeSpan.FromSeconds(61));
        var podadas = await cache.PruneAsync();

        Assert.That(podadas, Is.EqualTo(2));
        Assert.That(_store.IndexedKeys, Is.Empty);
        Assert.That(await cache.PruneAsync(), Is.EqualTo(0));
    }

    /// <summary>
    /// Dado que se guarda una entrada, cuando la vida máxima no es positiva,
    /// entonces se rechaza con ArgumentException.
    /// </summary>
    [Test]
    public void Set_VidaMaximaNoPositiva_LanzaExcepcion() {
        var cache = CreateService();

        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => cache.SetAsync("clave", "valor", TimeSpan.Zero));
    }

    /// <summary>
    /// Dado que la configuración es inválida, cuando se crea el servicio,
    /// entonces se rechaza con ArgumentException.
    /// </summary>
    [Test]
    public void Constructor_ConfiguracionInvalida_LanzaExcepcion() {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateService(o => o.Capacity = 0));
    }
}
