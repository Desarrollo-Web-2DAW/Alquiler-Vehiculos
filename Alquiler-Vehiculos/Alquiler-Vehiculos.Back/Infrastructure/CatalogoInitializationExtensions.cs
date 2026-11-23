using Alquiler_Vehiculos.Data.Seed;
using Alquiler_Vehiculos.Data.Vehiculo;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
/// Inicializa la base de datos del catálogo al arrancar la aplicación.
/// Aplica las migraciones pendientes y, en desarrollo, ejecuta las semillas.
/// </summary>
public static class CatalogoInitializationExtensions {

    /// <summary>
    /// Aplica las migraciones del contexto de catálogo y siembra sus datos iniciales.
    /// El esquema se construye siempre con migraciones, nunca con <c>EnsureCreated</c>.
    /// </summary>
    /// <param name="app">Aplicación de la que se obtiene el contenedor de servicios.</param>
    /// <param name="isDevelopment">Indica si deben insertarse los datos de desarrollo.</param>
    public static async Task InitializeCatalogoAsync(this WebApplication app, bool isDevelopment) {
        Log.Information("Inicializando base de datos del catálogo...");

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PostgreDbContext>();

        // Aplica solo las migraciones pendientes, sin borrar los datos existentes.
        await context.Database.MigrateAsync();

        if (!isDevelopment) {
            Log.Information("Catálogo: sin semillas fuera de Development.");
            return;
        }

        var mongoContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        await CatalogoSeeder.SeedAsync(context, mongoContext, app.Configuration, app.Environment);
    }
}
