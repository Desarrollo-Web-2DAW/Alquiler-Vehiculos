using Alquiler_Vehiculos.Services.Oficina;

namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
/// Registro en DI del módulo de Oficina única (RF-11).
/// </summary>
public static class OficinaConfig {
    /// <summary>Registra <see cref="IOficinaService"/> en el contenedor.</summary>
    public static IServiceCollection AddOficinaServices(this IServiceCollection services) {
        services.AddScoped<IOficinaService, OficinaService>();
        return services;
    }
}
