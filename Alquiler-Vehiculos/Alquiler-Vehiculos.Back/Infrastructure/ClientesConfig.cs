using Alquiler_Vehiculos.Repositories.Clientes;
using Alquiler_Vehiculos.Services.Usuarios.Clientes;

namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
/// Configuración de los clientes en el contenedor de dependencias (RF-13, RF-14).
/// </summary>
/// <remarks>
/// Los validadores no se registran aquí: los recoge <c>AddValidatorsFromAssemblyContaining</c>
/// en <c>Program.cs</c>, que registra los de todos los módulos.
/// </remarks>
public static class ClientesConfig {
    /// <summary>
    /// Registra el repositorio y el servicio de clientes. Scoped, porque el repositorio
    /// usa el DbContext, que vive lo que dura cada petición.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <returns>La misma colección, para poder encadenar.</returns>
    public static IServiceCollection AddClientes(this IServiceCollection services) {
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IClienteService, ClienteService>();
        return services;
    }
}