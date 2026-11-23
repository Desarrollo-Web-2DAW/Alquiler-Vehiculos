using Alquiler_Vehiculos.Data.Seed.Storage;
using Alquiler_Vehiculos.Data.Seed.Vehiculo;
using Alquiler_Vehiculos.Data.Vehiculo;
using Serilog;

namespace Alquiler_Vehiculos.Data.Seed;

/// <summary>
/// Ejecuta las semillas del catálogo en el orden que exigen sus relaciones.
/// Cada seeder comprueba su tabla o colección y no duplica datos existentes.
/// </summary>
public static class CatalogoSeeder {

    /// <summary>
    /// Inserta fotos, marcas, concesionarios y vehículos. Requiere el esquema
    /// creado y las imágenes de <c>SeedImages</c> preparadas en el almacenamiento.
    /// </summary>
    /// <param name="context">Contexto PostgreSQL del catálogo.</param>
    /// <param name="mongoContext">Contexto MongoDB de las marcas.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <param name="env">Entorno del que se obtiene la raíz web o del proyecto.</param>
    public static async Task SeedAsync(PostgreDbContext context, MongoDbContext mongoContext,
        IConfiguration configuration, IWebHostEnvironment env) {
        Log.Information("Ejecutando semillas del catálogo...");

        // Las fotos y los catálogos deben existir antes de asociarlos a los vehículos.
        await FotoSeeder.SeedAsync(context, configuration, env);
        await MarcaSeeder.SeedAsync(mongoContext);
        await ConcesionarioSeeder.SeedAsync(context);
        await VehiculoSeeder.SeedAsync(context, mongoContext, configuration);

        Log.Information("Semillas del catálogo finalizadas.");
    }
}
