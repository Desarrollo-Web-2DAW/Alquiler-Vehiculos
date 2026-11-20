using Alquiler_Vehiculos.Cache;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Alquiler_Vehiculos.Tests.Unit.Cache;

/// <summary>
/// Tests unitarios de la caché LRU con TTL mixto (LruCacheService)
/// sobre un almacén falso, sin Redis. Requisito cubierto: RNF-02
/// (y la invalidación selectiva que después usará CU-15).
/// </summary>
[Category("Unit")]
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

    #region Tests GetAsync

    /// <summary>
    /// Dado que la caché está vacía, cuando se consulta una clave,
    /// entonces devuelve default (fallo de caché).
    /// </summary>
    [Test]
    public async Task GetAsync_ClaveInexistente_DevuelveDefault() {
        // Arrange
        var cache = CreateService();

        // Act
        var result = await cache.GetAsync<string>("clave");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Dado que se guarda un valor, cuando se consulta la misma clave,
    /// entonces devuelve el valor (acierto de caché).
    /// </summary>
    [Test]
    public async Task GetAsync_ClaveRecienGuardada_DevuelveElValor() {
        // Arrange
        var cache = CreateService();

        // Act
        await cache.SetAsync("clave", "valor");
        var result = await cache.GetAsync<string>("clave");

        // Assert
        result.Should().Be("valor");
    }

    /// <summary>
    /// Dado que una entrada no se vuelve a consultar, cuando pasa su ventana móvil,
    /// entonces caduca y ya no se devuelve.
    /// </summary>
    [Test]
    public async Task GetAsync_SinAccesosDuranteLaVentana_LaEntradaCaduca() {
        // Arrange
        var cache = CreateService();
        await cache.SetAsync("clave", "valor");

        // Act
        _clock.Advance(TimeSpan.FromSeconds(61));
        var result = await cache.GetAsync<string>("clave");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Dado que una entrada se consulta antes de caducar, cuando se consulta,
    /// entonces la ventana móvil se renueva y sobrevive más allá del plazo original.
    /// </summary>
    [Test]
    public async Task GetAsync_LecturasDentroDeLaVentana_RenuevaLaVentanaMovil() {
        // Arrange
        var cache = CreateService();
        await cache.SetAsync("clave", "valor");

        // Act & Assert
        _clock.Advance(TimeSpan.FromSeconds(50));
        (await cache.GetAsync<string>("clave")).Should().Be("valor");

        // 100 s desde la escritura: solo sigue viva porque la lectura anterior la renovó
        _clock.Advance(TimeSpan.FromSeconds(50));
        (await cache.GetAsync<string>("clave")).Should().Be("valor");

        _clock.Advance(TimeSpan.FromSeconds(61));
        (await cache.GetAsync<string>("clave")).Should().BeNull();
    }

    /// <summary>
    /// Dado que una entrada se consulta de forma continua, cuando alcanza su vida máxima,
    /// entonces caduca igualmente: la renovación nunca rebasa el deadline (TTL mixto).
    /// </summary>
    [Test]
    public async Task GetAsync_LecturasContinuas_CaducaAlcanzandoLaVidaMaxima() {
        // Arrange
        var cache = CreateService(); // vida máxima 600 s, ventana móvil 60 s
        await cache.SetAsync("clave", "valor");

        // Act & Assert: lectura cada 50 s, siempre dentro de la ventana móvil
        for (var i = 0; i < 11; i++) {
            _clock.Advance(TimeSpan.FromSeconds(50));
            (await cache.GetAsync<string>("clave")).Should().Be("valor", $"la lectura {i +  1} ocurre a los {50 * (i + 1)} s");
        }

        // 600 s desde la escritura: exactamente la vida máxima
        _clock.Advance(TimeSpan.FromSeconds(50));
        (await cache.GetAsync<string>("clave")).Should().BeNull();
    }

    /// <summary>
    /// Dado que el payload guardado no es una envolvente válida, cuando se consulta,
    /// entonces devuelve default sin lanzar excepción.
    /// </summary>
    [Test]
    public async Task GetAsync_EnvolventeCorrupta_DevuelveDefault() {
        // Arrange
        var cache = CreateService();
        await _store.StoreAsync("clave", "null", TimeSpan.FromMinutes(1), score: 1, tags: [], capacity: 10);

        // Act
        var result = await cache.GetAsync<string>("clave");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Dado que una entrada agotó su vida máxima pero su valor sigue vivo físicamente,
    /// cuando se consulta, entonces se retira de golpe y devuelve default.
    /// </summary>
    [Test]
    public async Task GetAsync_DeadlineAgotadoPeroValorFisicoVivo_LaEliminaYDevuelveDefault() {
        // Arrange
        var cache = CreateService();
        await cache.SetAsync("clave", "valor");
        // el valor físico vive más allá del deadline (posible desfase de reloj en Redis)
        await _store.RenewAsync("clave", TimeSpan.FromHours(1), score: 1);
        _clock.Advance(TimeSpan.FromSeconds(601));

        // Act
        var result = await cache.GetAsync<string>("clave");

        // Assert
        result.Should().BeNull();
        (await _store.ExistsAsync("clave")).Should().BeFalse("la entrada caducada por vida máxima se retira de golpe");
    }

    #endregion

    #region Tests SetAsync

    /// <summary>
    /// Dado que la caché supera su capacidad, cuando se guarda una entrada nueva,
    /// entonces se expulsa la menos usada recientemente (política LRU)
    /// y las más recientes siguen disponibles.
    /// </summary>
    [Test]
    public async Task Set_SuperaLaCapacidad_ExpulsaLaMenosUsada() {
        // Arrange
        var cache = CreateService(); // capacidad 3
        await cache.SetAsync("a", "valor-a");
        _clock.Advance(TimeSpan.FromSeconds(1));
        await cache.SetAsync("b", "valor-b");
        _clock.Advance(TimeSpan.FromSeconds(1));
        await cache.SetAsync("c", "valor-c");
        _clock.Advance(TimeSpan.FromSeconds(1));

        // "a" vuelve a ser la más usada: "b" queda como la menos usada
        (await cache.GetAsync<string>("a")).Should().Be("valor-a");
        _clock.Advance(TimeSpan.FromSeconds(1));

        // Act
        await cache.SetAsync("d", "valor-d");

        // Assert
        (await cache.ExistsAsync("a")).Should().BeTrue();
        (await cache.ExistsAsync("b")).Should().BeFalse("b es la menos usada y debe expulsarse");
        (await cache.ExistsAsync("c")).Should().BeTrue();
        (await cache.ExistsAsync("d")).Should().BeTrue();
    }

    /// <summary>
    /// Dado que se guarda una entrada con vida máxima propia, cuando pasa ese tiempo,
    /// entonces caduca según esa vida y no según la de las opciones.
    /// </summary>
    [Test]
    public async Task Set_ConVidaMaximaPropia_AplicaEsaVidaMaxima() {
        // Arrange
        var cache = CreateService(); // ventana móvil 60 s en las opciones

        // Act
        await cache.SetAsync("clave", "valor", TimeSpan.FromSeconds(10));
        _clock.Advance(TimeSpan.FromSeconds(11));
        var result = await cache.GetAsync<string>("clave");

        // Assert
        result.Should().BeNull("la vida propia de 10 s manda sobre la ventana móvil de las opciones");
    }

    /// <summary>
    /// Dado que una clave se reescribe con otras etiquetas, cuando se invalida la etiqueta vieja,
    /// entonces la entrada sobrevive; al invalidar la nueva, se elimina.
    /// </summary>
    [Test]
    public async Task Set_ReescribirClave_ActualizaValorYEtiquetas() {
        // Arrange
        var cache = CreateService();
        await cache.SetAsync("k", "valor-viejo", tags: ["t1"]);
        await cache.SetAsync("k", "valor-nuevo", tags: ["t2"]);

        // Act & Assert
        await cache.RemoveByTagAsync("t1");
        (await cache.GetAsync<string>("k")).Should().Be("valor-nuevo");

        await cache.RemoveByTagAsync("t2");
        (await cache.ExistsAsync("k")).Should().BeFalse();
    }

    /// <summary>
    /// Dado que se guarda una entrada, cuando la vida máxima no es positiva,
    /// entonces se rechaza con ArgumentException.
    /// </summary>
    [Test]
    public async Task Set_VidaMaximaNoPositiva_LanzaExcepcion() {
        // Arrange
        var cache = CreateService();
        Func<Task> act = () => cache.SetAsync("clave", "valor", TimeSpan.Zero);

        // Act & Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Tests GetOrSetAsync

    /// <summary>
    /// Dado que una clave no está en caché, cuando se usa GetOrSet,
    /// entonces se llama a la fábrica; en las consultas siguientes no vuelve a llamarse.
    /// </summary>
    [Test]
    public async Task GetOrSet_ClaveInexistente_LlamaUnaVezALaFactory() {
        // Arrange
        var cache = CreateService();
        var llamadas = 0;
        Task<string> Factory() {
            llamadas++;
            return Task.FromResult("calculado");
        }

        // Act
        var primero = await cache.GetOrSetAsync("clave", Factory);
        var segundo = await cache.GetOrSetAsync("clave", Factory);

        // Assert
        primero.Should().Be("calculado");
        segundo.Should().Be("calculado");
        llamadas.Should().Be(1);
    }

    /// <summary>
    /// Dado que la fábrica devuelve null, cuando se usa GetOrSet,
    /// entonces no se guarda nada en la caché.
    /// </summary>
    [Test]
    public async Task GetOrSet_FactoryDevuelveNull_NoGuardaEnLaCache() {
        // Arrange
        var cache = CreateService();

        // Act
        var result = await cache.GetOrSetAsync<string>("clave", () => Task.FromResult<string?>(null)!);

        // Assert
        result.Should().BeNull();
        (await cache.ExistsAsync("clave")).Should().BeFalse();
    }

    /// <summary>
    /// Dado que la fábrica es null, cuando se usa GetOrSet,
    /// entonces se rechaza con ArgumentException.
    /// </summary>
    [Test]
    public async Task GetOrSet_FactoryNula_LanzaExcepcion() {
        // Arrange
        var cache = CreateService();
        Func<Task> act = () => cache.GetOrSetAsync<string>("clave", null!);

        // Act & Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Tests invalidación y poda

    /// <summary>
    /// Dado que una clave está guardada, cuando se elimina,
    /// entonces ya no existe en la caché.
    /// </summary>
    [Test]
    public async Task Remove_ClaveGuardada_LaEliminaDeLaCache() {
        // Arrange
        var cache = CreateService();
        await cache.SetAsync("clave", "valor");

        // Act
        await cache.RemoveAsync("clave");

        // Assert
        (await cache.ExistsAsync("clave")).Should().BeFalse();
    }

    /// <summary>
    /// Dado que varias entradas tienen etiquetas distintas, cuando se invalida una etiqueta,
    /// entonces solo se eliminan las entradas afectadas (invalidación selectiva, D4).
    /// </summary>
    [Test]
    public async Task RemoveByTag_VariasEtiquetas_EliminaSoloLasAfectadas() {
        // Arrange
        var cache = CreateService();
        await cache.SetAsync("k1", "valor-1", tags: ["vehiculo:1"]);
        await cache.SetAsync("k2", "valor-2", tags: ["vehiculo:2"]);

        // Act
        await cache.RemoveByTagAsync("vehiculo:1");

        // Assert
        (await cache.ExistsAsync("k1")).Should().BeFalse();
        (await cache.ExistsAsync("k2")).Should().BeTrue();
    }

    /// <summary>
    /// Dado que la caché tiene entradas, cuando se vacía,
    /// entonces no queda ninguna.
    /// </summary>
    [Test]
    public async Task Clear_ConEntradas_EliminaTodasLasClaves() {
        // Arrange
        var cache = CreateService();
        await cache.SetAsync("k1", "valor-1", tags: ["t1"]);
        await cache.SetAsync("k2", "valor-2", tags: ["t2"]);

        // Act
        await cache.ClearAsync();

        // Assert
        (await cache.ExistsAsync("k1")).Should().BeFalse();
        (await cache.ExistsAsync("k2")).Should().BeFalse();
    }

    /// <summary>
    /// Dado que hay entradas caducadas aún indexadas, cuando se poda a mano,
    /// entonces se limpian del índice y una segunda poda no encuentra nada
    /// (poda invocable sin esperar al timer, criterio de RF-40).
    /// </summary>
    [Test]
    public async Task Prune_EntradasCaducadas_LasLimpiaDelIndice() {
        // Arrange
        var cache = CreateService();
        await cache.SetAsync("k1", "valor-1");
        await cache.SetAsync("k2", "valor-2");
        _clock.Advance(TimeSpan.FromSeconds(61));

        // Act
        var podadas = await cache.PruneAsync();

        // Assert
        podadas.Should().Be(2);
        _store.IndexedKeys.Should().BeEmpty();
        (await cache.PruneAsync()).Should().Be(0);
    }

    #endregion

    #region Tests de construcción

    /// <summary>
    /// Dado que la configuración es inválida, cuando se crea el servicio,
    /// entonces se rechaza con ArgumentException.
    /// </summary>
    [Test]
    public void Constructor_ConfiguracionInvalida_LanzaExcepcion() {
        // Arrange
        Action act = () => CreateService(o => o.Capacity = 0);

        // Act & Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Dado que alguna dependencia es null, cuando se crea el servicio,
    /// entonces se rechaza con ArgumentException.
    /// </summary>
    [Test]
    public void Constructor_DependenciasNulas_LanzaExcepcion() {
        // Arrange
        var options = Options.Create(new CacheOptions());
        Action sinStore = () => new LruCacheService(null!, options, _clock, NullLogger<LruCacheService>.Instance);
        Action sinReloj = () => new LruCacheService(_store, options, null!, NullLogger<LruCacheService>.Instance);
        Action sinLogger = () => new LruCacheService(_store, options, _clock, null!);

        // Act & Assert
        sinStore.Should().Throw<ArgumentNullException>();
        sinReloj.Should().Throw<ArgumentNullException>();
        sinLogger.Should().Throw<ArgumentNullException>();
    }

    #endregion
}
