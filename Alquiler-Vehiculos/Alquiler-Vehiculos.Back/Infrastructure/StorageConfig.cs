using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Services.Storage;
using Serilog;

namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
/// Configuración del registro del servicio de almacenamiento de imágenes.
/// </summary>
public static class StorageConfig {
    /// <summary>
    /// Registra FileSystemStorageService como implementación de IStorageService con duración Scoped.
    /// </summary>
    /// <param name="services">Colección de servicios del contenedor de dependencias.</param>
    /// <param name="configuration">Configuración de la app.</param>
    /// <returns>La colección de servicios para permitir encadenar otros registros.</returns>
    public static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration) {
        Log.Information("Registrando servicio de almacenamiento de imagenes..");
        
        services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName));
        
        return services.AddScoped<IStorageService, StorageService>();
    }
}