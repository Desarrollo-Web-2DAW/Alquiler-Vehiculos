using Alquiler_Vehiculos.Dto.Taller;
using Alquiler_Vehiculos.Repositories.Taller;
using Alquiler_Vehiculos.Services.Taller;
using Alquiler_Vehiculos.Validators.Taller;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
/// Registro del módulo de partes de taller en el contenedor (RF-08).
/// </summary>
public static class TallerConfig {
    /// <summary>
    /// Registra el repositorio, el servicio y el validador del taller.
    /// El repositorio en memoria se sustituirá por el de EF Core cuando el grupo
    /// fije la base de datos transaccional junto al módulo de vehículos.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <returns>La misma colección, para encadenar.</returns>
    public static IServiceCollection AddTallerServices(this IServiceCollection services) {
        services.TryAddSingleton<IParteTallerRepository, InMemoryParteTallerRepository>();
        services.TryAddScoped<IParteTallerService, ParteTallerService>();
        services.TryAddSingleton<IValidator<AbrirParteDto>, AbrirParteValidator>();
        return services;
    }
}
