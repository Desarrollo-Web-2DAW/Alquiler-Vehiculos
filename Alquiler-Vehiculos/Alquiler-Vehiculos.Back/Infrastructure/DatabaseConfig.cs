using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Repositories.Clientes;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
/// Configuración de la base de datos transaccional PostgreSQL con EF Core.
/// </summary>
public static class DatabaseConfig {
    /// <summary>
    /// Registra <see cref="AppDbContextUsuarios"/> y los repositorios asociados.
    /// Aplica fail-fast si falta la cadena de conexión.
    /// </summary>
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment) {
        Log.Information("Configurando PostgreSQL...");

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString)) {
            throw new InvalidOperationException(
                "Falta 'ConnectionStrings:DefaultConnection'. " +
                (environment.IsDevelopment()
                    ? "Configúrala en appsettings.Development.json o en User Secrets."
                    : "En producción debe inyectarse por variable de entorno (ConnectionStrings__DefaultConnection)."));
        }

        services.AddDbContext<AppDbContextUsuarios>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IClienteRepository, ClienteRepository>();

        Log.Information("PostgreSQL configurado correctamente.");
        return services;
    }
}
