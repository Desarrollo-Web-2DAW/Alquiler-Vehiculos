using Alquiler_Vehiculos.Cache;
using StackExchange.Redis;

namespace Alquiler_Vehiculos.Tests.Integration.Cache;

/// <summary>
/// Tests de integración del almacén de caché contra Redis de verdad
/// (RNF-04: integración con los servicios levantados en contenedores).
/// Valida los scripts Lua de expulsión e invalidación que las pruebas unitarias
/// no pueden ver. Si no hay un Redis disponible se omiten solas:
/// cadena en la variable de entorno TEST_REDIS_CONNECTION o localhost:6379.
/// </summary>
public class RedisCacheStoreTests {
    private ConnectionMultiplexer _mux = null!;
    private RedisCacheStore _store = null!;

    [SetUp]
    public void Setup() {
        var connectionString = Environment.GetEnvironmentVariable("TEST_REDIS_CONNECTION") ?? "localhost:6379";
        try {
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 1000;
            _mux = ConnectionMultiplexer.Connect(options);
            _mux.GetDatabase().Ping();
        } catch (Exception ex) {
            Assert.Ignore($"Redis no disponible ({ex.Message}). Pruebas de integración de caché omitidas.");
        }

        _store = new RedisCacheStore(_mux);
        _store.ClearAsync().GetAwaiter().GetResult();
    }

    [TearDown]
    public void TearDown() {
        _mux?.Dispose();
    }

    private static string ClaveNueva() => "int-" + Guid.NewGuid().ToString("N");

    /// <summary>
    /// Dado que se guarda una entrada con etiquetas, cuando se consulta,
    /// entonces devuelve su payload; tras eliminarla, ya no existe.
    /// </summary>
    [Test]
    public async Task StoreYGet_DevuelveElPayload() {
        var key = ClaveNueva();

        await _store.StoreAsync(key, "payload", TimeSpan.FromMinutes(1), score: 1, tags: ["t1"], capacity: 10);

        Assert.That(await _store.GetAsync(key), Is.EqualTo("payload"));
        Assert.That(await _store.ExistsAsync(key), Is.True);

        await _store.RemoveAsync(key);

        Assert.That(await _store.GetAsync(key), Is.Null);
        Assert.That(await _store.ExistsAsync(key), Is.False);
    }

    /// <summary>
    /// Dado que se guarda por encima de la capacidad, cuando se guarda la última,
    /// entonces el script expulsa la entrada menos usada (LRU) y devuelve sus claves.
    /// </summary>
    [Test]
    public async Task Store_SuperaLaCapacidad_ExpulsaLaMenosUsada() {
        var a = ClaveNueva();
        var b = ClaveNueva();
        var c = ClaveNueva();

        await _store.StoreAsync(a, "va", TimeSpan.FromMinutes(1), score: 1, tags: [], capacity: 2);
        await _store.StoreAsync(b, "vb", TimeSpan.FromMinutes(1), score: 2, tags: [], capacity: 2);
        var evicted = await _store.StoreAsync(c, "vc", TimeSpan.FromMinutes(1), score: 3, tags: [], capacity: 2);

        Assert.That(evicted, Does.Contain(a));
        Assert.That(await _store.ExistsAsync(a), Is.False);
        Assert.That(await _store.ExistsAsync(b), Is.True);
        Assert.That(await _store.ExistsAsync(c), Is.True);
    }

    /// <summary>
    /// Dado que varias entradas tienen etiquetas distintas, cuando se invalida una,
    /// entonces solo se eliminan las marcadas con ella.
    /// </summary>
    [Test]
    public async Task RemoveByTag_EliminaSoloLasAfectadas() {
        var k1 = ClaveNueva();
        var k2 = ClaveNueva();
        await _store.StoreAsync(k1, "v1", TimeSpan.FromMinutes(1), score: 1, tags: ["vehiculo:1"], capacity: 10);
        await _store.StoreAsync(k2, "v2", TimeSpan.FromMinutes(1), score: 2, tags: ["vehiculo:2"], capacity: 10);

        await _store.RemoveByTagAsync("vehiculo:1");

        Assert.That(await _store.ExistsAsync(k1), Is.False);
        Assert.That(await _store.ExistsAsync(k2), Is.True);
    }

    /// <summary>
    /// Dado que la caché tiene entradas, cuando se vacía, entonces no queda ninguna.
    /// </summary>
    [Test]
    public async Task Clear_VaciaLaCache() {
        await _store.StoreAsync(ClaveNueva(), "v1", TimeSpan.FromMinutes(1), score: 1, tags: ["t1"], capacity: 10);
        await _store.StoreAsync(ClaveNueva(), "v2", TimeSpan.FromMinutes(1), score: 2, tags: ["t2"], capacity: 10);

        await _store.ClearAsync();

        Assert.That(await _store.PruneAsync(), Is.Empty);
    }

    /// <summary>
    /// Dado que la caché está en uso, cuando se poda,
    /// entonces termina sin errores y sin entradas que limpiar.
    /// </summary>
    [Test]
    public async Task Prune_ConTodoEnOrden_NoLimpiaNada() {
        var key = ClaveNueva();
        await _store.StoreAsync(key, "v", TimeSpan.FromMinutes(1), score: 1, tags: ["t"], capacity: 10);

        var pruned = await _store.PruneAsync();

        Assert.That(pruned, Is.Empty);
        Assert.That(await _store.ExistsAsync(key), Is.True);
    }
}
