using Alquiler_Vehiculos.Cache;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Alquiler_Vehiculos.Tests.Unit.Cache;

/// <summary>
/// Tests unitarios del registro de la caché en el contenedor (CacheConfig):
/// servicios registrados y enlace de las opciones con appsettings (RNF-02, RNF-05).
/// Se comprueban los descriptores sin resolver dependencias de Redis.
/// </summary>
[Category("Unit")]
public class CacheConfigTests {
    private static IServiceCollection CrearServicios() {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Cache:Capacity"] = "10",
                ["Cache:SlidingExpirationSeconds"] = "30",
                ["Cache:AbsoluteMaxLifetimeSeconds"] = "120",
                ["Cache:PruneIntervalSeconds"] = "15",
                ["Redis:ConnectionString"] = "localhost:6379"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddCache(configuration);
        return services;
    }

    /// <summary>
    /// Dado que se registra la caché, cuando se inspeccionan los servicios,
    /// entonces están el almacén, el servicio LRU y el reloj.
    /// </summary>
    [Test]
    public void AddCache_RegistraAlmacenServicioYReloj() {
        // Arrange
        var services = CrearServicios();

        // Act & Assert
        services.Should().Contain(d => d.ServiceType == typeof(ICacheStore) && d.ImplementationType == typeof(RedisCacheStore));
        services.Should().Contain(d => d.ServiceType == typeof(ICacheService) && d.ImplementationType == typeof(LruCacheService));
        services.Should().Contain(d => d.ServiceType == typeof(TimeProvider));
    }

    /// <summary>
    /// Dado que se registra la caché, cuando se inspeccionan los servicios,
    /// entonces la tarea de poda queda registrada como servicio en segundo plano.
    /// </summary>
    [Test]
    public void AddCache_RegistraLaTareaDePodaEnSegundoPlano() {
        // Arrange
        var services = CrearServicios();

        // Act & Assert
        services.Should().Contain(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(CachePruneService));
    }

    /// <summary>
    /// Dado que la configuración tiene la sección "Cache", cuando se registran
    /// las opciones, entonces se enlazan con sus valores.
    /// </summary>
    [Test]
    public void AddCache_LasOpcionesSeEnlazanConLaConfiguracion() {
        // Arrange
        var services = CrearServicios();

        // Act
        using var provider = services.BuildServiceProvider();
        var opciones = provider.GetRequiredService<IOptions<CacheOptions>>().Value;

        // Assert
        opciones.Capacity.Should().Be(10);
        opciones.SlidingExpirationSeconds.Should().Be(30);
        opciones.AbsoluteMaxLifetimeSeconds.Should().Be(120);
        opciones.PruneIntervalSeconds.Should().Be(15);
        opciones.SlidingExpiration.Should().Be(TimeSpan.FromSeconds(30));
        opciones.AbsoluteMaxLifetime.Should().Be(TimeSpan.FromSeconds(120));
        opciones.PruneInterval.Should().Be(TimeSpan.FromSeconds(15));
    }

    /// <summary>
    /// Dado que no hay sección "Cache" en la configuración, cuando se registran
    /// las opciones, entonces se usan los valores por defecto.
    /// </summary>
    [Test]
    public void AddCache_SinSeccion_UsaLosValoresPorDefecto() {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        // Act
        services.AddCache(configuration);
        using var provider = services.BuildServiceProvider();
        var opciones = provider.GetRequiredService<IOptions<CacheOptions>>().Value;

        // Assert
        opciones.Capacity.Should().Be(500);
        opciones.SlidingExpirationSeconds.Should().Be(60);
        opciones.AbsoluteMaxLifetimeSeconds.Should().Be(600);
        opciones.PruneIntervalSeconds.Should().Be(60);
    }
}
