using Alquiler_Vehiculos.Cache;
using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Services.Storage;
using Alquiler_Vehiculos.Services.Usuarios.Auth;
using Microsoft.Extensions.Options;
using Serilog;

namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
/// Configuración de servicios de negocio.
/// </summary>
public static class ServicesConfig {
    /// <summary>
    /// Registra todos los servicios de negocio en el contenedor de dependencias.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">La configuración de la aplicación (<see cref="IConfiguration"/>) disponible.</param>
    /// <returns>IServiceCollection para encadenar.</returns>
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration) {
        Log.Information("Registrando servicios...");
        services.Configure<StorageSettings>(
            configuration.GetSection(StorageSettings.SectionName)
        );

        Log.Information("Servicios registrados correctamente.");
        return services;
    }
}