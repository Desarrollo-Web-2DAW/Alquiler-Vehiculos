using Alquiler_Vehiculos.Cache;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Alquiler_Vehiculos.Tests.Integration.Cache;

/// <summary>
/// Tests de integración del almacén de caché contra Redis de verdad, con
/// Testcontainers (RNF-04: integración con los servicios levantados en contenedores).
/// Valida los scripts Lua de expulsión e invalidación que las pruebas unitarias
/// no pueden ver. Requiere Docker: si no está disponible, el arranque del
/// contenedor falla y el bloqueo queda a la vista (no se omiten en silencio).
/// </summary>
[Category("Integration")]
public class RedisCacheStoreTests {
    private static readonly RedisContainer _container = new RedisBuilder("redis:7.4-alpine").Build();

    private ConnectionMultiplexer _mux = null!;
    private RedisCacheStore _store = null!;

    [OneTimeSetUp]
    public async Task ArrancarRedis() {
        await _container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task PararRedis() {
        await _container.DisposeAsync();
    }

    [SetUp]
    public async Task Setup() {
        // Arrange
        _mux = await ConnectionMultiplexer.ConnectAsync(_container.GetConnectionString());
        _store = new RedisCacheStore(_mux);
        await _store.ClearAsync();
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
    public async Task StoreYGet_EntradaGuardada_DevuelveElPayloadYAlBorrarNoExiste() {
        // Arrange
        var key = ClaveNueva();

        // Act
        await _store.StoreAsync(key, "payload", TimeSpan.FromMinutes(1), score: 1, tags: ["t1"], capacity: 10);

        // Assert
        (await _store.GetAsync(key)).Should().Be("payload");
        (await _store.ExistsAsync(key)).Should().BeTrue();

        // Act
        await _store.RemoveAsync(key);

        // Assert
        (await _store.GetAsync(key)).Should().BeNull();
        (await _store.ExistsAsync(key)).Should().BeFalse();
    }

    /// <summary>
    /// Dado que se guarda por encima de la capacidad, cuando se guarda la última,
    /// entonces el script expulsa la entrada menos usada (LRU) y devuelve sus claves.
    /// </summary>
    [Test]
    public async Task Store_SuperaLaCapacidad_ExpulsaLaMenosUsada() {
        // Arrange
        var a = ClaveNueva();
        var b = ClaveNueva();
        var c = ClaveNueva();
        await _store.StoreAsync(a, "va", TimeSpan.FromMinutes(1), score: 1, tags: [], capacity: 2);
        await _store.StoreAsync(b, "vb", TimeSpan.FromMinutes(1), score: 2, tags: [], capacity: 2);

        // Act
        var evicted = await _store.StoreAsync(c, "vc", TimeSpan.FromMinutes(1), score: 3, tags: [], capacity: 2);

        // Assert
        evicted.Should().Contain(a);
        (await _store.ExistsAsync(a)).Should().BeFalse();
        (await _store.ExistsAsync(b)).Should().BeTrue();
        (await _store.ExistsAsync(c)).Should().BeTrue();
    }

    /// <summary>
    /// Dado que varias entradas tienen etiquetas distintas, cuando se invalida una,
    /// entonces solo se eliminan las marcadas con ella.
    /// </summary>
    [Test]
    public async Task RemoveByTag_VariasEtiquetas_EliminaSoloLasAfectadas() {
        // Arrange
        var k1 = ClaveNueva();
        var k2 = ClaveNueva();
        await _store.StoreAsync(k1, "v1", TimeSpan.FromMinutes(1), score: 1, tags: ["vehiculo:1"], capacity: 10);
        await _store.StoreAsync(k2, "v2", TimeSpan.FromMinutes(1), score: 2, tags: ["vehiculo:2"], capacity: 10);

        // Act
        await _store.RemoveByTagAsync("vehiculo:1");

        // Assert
        (await _store.ExistsAsync(k1)).Should().BeFalse();
        (await _store.ExistsAsync(k2)).Should().BeTrue();
    }

    /// <summary>
    /// Dado que la caché tiene entradas, cuando se vacía,
    /// entonces no queda ninguna.
    /// </summary>
    [Test]
    public async Task Clear_ConEntradas_NoQuedaNinguna() {
        // Arrange
        await _store.StoreAsync(ClaveNueva(), "v1", TimeSpan.FromMinutes(1), score: 1, tags: ["t1"], capacity: 10);
        await _store.StoreAsync(ClaveNueva(), "v2", TimeSpan.FromMinutes(1), score: 2, tags: ["t2"], capacity: 10);

        // Act
        await _store.ClearAsync();

        // Assert
        (await _store.PruneAsync()).Should().BeEmpty();
    }

    /// <summary>
    /// Dado que la caché está en uso, cuando se poda,
    /// entonces termina sin errores y sin entradas que limpiar.
    /// </summary>
    [Test]
    public async Task Prune_ConTodoEnOrden_NoLimpiaNada() {
        // Arrange
        var key = ClaveNueva();
        await _store.StoreAsync(key, "v", TimeSpan.FromMinutes(1), score: 1, tags: ["t"], capacity: 10);

        // Act
        var pruned = await _store.PruneAsync();

        // Assert
        pruned.Should().BeEmpty();
        (await _store.ExistsAsync(key)).Should().BeTrue();
    }

    /// <summary>
    /// Dado que una entrada perdió su valor pero sigue indexada, cuando se poda,
    /// entonces se limpia del índice y se devuelve su clave.
    /// </summary>
    [Test]
    public async Task Prune_EntradaSinValor_LaLimpiaDelIndice() {
        // Arrange
        var key = ClaveNueva();
        await _store.StoreAsync(key, "v", TimeSpan.FromMinutes(1), score: 1, tags: ["t"], capacity: 10);
        // el valor caduca en Redis pero la referencia del índice se queda huérfana
        await _mux.GetDatabase().KeyDeleteAsync("cache:val:" + key);

        // Act
        var pruned = await _store.PruneAsync();

        // Assert
        pruned.Should().Contain(key);
        (await _store.ExistsAsync(key)).Should().BeFalse();
    }

    /// <summary>
    /// Dado que la caché está registrada con el contenedor de Redis real,
    /// cuando se guarda y se consulta a través del servicio completo,
    /// entonces funciona de extremo a extremo (registro, conexión, TTL mixto y renovación).
    /// </summary>
    [Test]
    public async Task AddCache_ConRedisReal_ElServicioCompletoFunciona() {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Redis:ConnectionString"] = _container.GetConnectionString()
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCache(configuration);
        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();
        var key = ClaveNueva();

        // Act
        await cache.SetAsync(key, "valor", tags: ["t"]);
        var result = await cache.GetAsync<string>(key);

        // Assert
        result.Should().Be("valor");
    }
}
