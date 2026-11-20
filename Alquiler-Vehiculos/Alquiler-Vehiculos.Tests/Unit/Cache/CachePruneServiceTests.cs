using Alquiler_Vehiculos.Cache;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Alquiler_Vehiculos.Tests.Unit.Cache;

/// <summary>
/// Tests unitarios de la tarea de poda en segundo plano (CachePruneService).
/// Requisito cubierto: RF-40 (el manejador se puede lanzar y comprobar sin
/// depender del timer global) y la resiliencia de la tarea ante fallos.
/// </summary>
[Category("Unit")]
public class CachePruneServiceTests {
    /// <summary>
    /// Doble de la caché que cuenta las podas y puede fallar o bloquearse a demanda.
    /// </summary>
    private sealed class CacheStub : ICacheService {
        public int Poda { get; private set; }
        public int ResultadoPoda { get; set; }
        public bool FallaEnLaPoda { get; set; }
        public bool BloqueaEnLaPoda { get; set; }

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) => Task.FromResult<T?>(default);
        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, IEnumerable<string>? tags = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string key, CancellationToken ct = default) => Task.FromResult(false);
        public Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, IEnumerable<string>? tags = null, CancellationToken ct = default) => Task.FromResult<T?>(default);
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveByTagAsync(string tag, CancellationToken ct = default) => Task.CompletedTask;
        public Task ClearAsync(CancellationToken ct = default) => Task.CompletedTask;

        public async Task<int> PruneAsync(CancellationToken ct = default) {
            Poda++;
            if (BloqueaEnLaPoda) {
                await Task.Delay(Timeout.Infinite, ct);
            }
            if (FallaEnLaPoda) {
                throw new InvalidOperationException("fallo de poda");
            }
            return ResultadoPoda;
        }
    }

    private static CachePruneService CrearServicio(CacheStub cache) {
        var options = Options.Create(new CacheOptions { PruneIntervalSeconds = 1 });
        return new CachePruneService(cache, options, NullLogger<CachePruneService>.Instance);
    }

    private static async Task EsperarAsync(Func<bool> condicion, string motivo, int timeoutMs = 5000) {
        var limite = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < limite) {
            if (condicion()) {
                return;
            }
            await Task.Delay(50);
        }
        Assert.Fail($"Tiempo agotado esperando: {motivo}");
    }

    /// <summary>
    /// Dado que la tarea está arrancada y hay entradas que podar, cuando pasa su intervalo,
    /// entonces lanza la poda de forma periódica y registra lo limpiado.
    /// </summary>
    [Test]
    public async Task ExecuteAsync_ConIntervalo_LanzaLaPodaPeriodicamente() {
        // Arrange
        var cache = new CacheStub { ResultadoPoda = 2 };
        var servicio = CrearServicio(cache);

        // Act
        await servicio.StartAsync(CancellationToken.None);
        await EsperarAsync(() => cache.Poda >= 2, "que la tarea lance la poda al menos dos veces");
        await servicio.StopAsync(CancellationToken.None);

        // Assert
        cache.Poda.Should().BeGreaterThanOrEqualTo(2);
    }

    /// <summary>
    /// Dado que la poda falla, cuando la tarea la lanza,
    /// entonces el error no tumba el bucle y se reintenta en el próximo tick.
    /// </summary>
    [Test]
    public async Task ExecuteAsync_PodaQueFalla_ElBucleSigueYSeReintenta() {
        // Arrange
        var cache = new CacheStub { FallaEnLaPoda = true };
        var servicio = CrearServicio(cache);

        // Act
        await servicio.StartAsync(CancellationToken.None);
        await EsperarAsync(() => cache.Poda >= 2, "que la tarea reintente la poda tras el fallo");
        await servicio.StopAsync(CancellationToken.None);

        // Assert
        cache.Poda.Should().BeGreaterThanOrEqualTo(2, "un fallo de poda no debe detener la tarea");
    }

    /// <summary>
    /// Dado que la tarea está en medio de una poda, cuando se detiene el servicio,
    /// entonces la poda se cancela y el bucle termina sin error.
    /// </summary>
    [Test]
    public async Task ExecuteAsync_ServicioDetenido_TerminaElBucleSinError() {
        // Arrange
        var cache = new CacheStub { BloqueaEnLaPoda = true };
        var servicio = CrearServicio(cache);

        // Act
        await servicio.StartAsync(CancellationToken.None);
        await EsperarAsync(() => cache.Poda >= 1, "que la tarea empiece una poda");
        await servicio.StopAsync(CancellationToken.None);

        // Assert
        cache.Poda.Should().BeGreaterThanOrEqualTo(1);
    }
}
