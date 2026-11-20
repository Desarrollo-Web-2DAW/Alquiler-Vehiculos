using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Alquiler_Vehiculos.Cache;

/// <summary>
/// Registro de la caché en el contenedor de dependencias.
/// Caché única sobre Redis, sin distinción de entornos (D1): la misma pieza
/// en desarrollo y en producción, con Redis incluido en el compose (RNF-05).
/// </summary>
public static class CacheConfig {
    /// <summary>
    /// Registra la caché LRU con TTL sobre Redis y su tarea de poda.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <returns>IServiceCollection para encadenar.</returns>
    public static IServiceCollection AddCache(this IServiceCollection services, IConfiguration configuration) {
        // opciones de la caché desde appsettings.json, sección "Cache"
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        // reloj inyectable para poder probar el TTL sin esperar tiempo real
        services.TryAddSingleton(TimeProvider.System);

        // Redis con arranque diferido: la aplicación levanta aunque Redis aún no esté disponible
        services.TryAddSingleton<IConnectionMultiplexer>(_ => {
            var connectionString = configuration["Redis:ConnectionString"] ?? "localhost:6379";
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });

        services.TryAddSingleton<ICacheStore, RedisCacheStore>();
        services.TryAddSingleton<ICacheService, LruCacheService>();
        services.AddHostedService<CachePruneService>();

        return services;
    }
}
