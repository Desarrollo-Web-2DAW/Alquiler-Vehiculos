using Alquiler_Vehiculos.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
/// Inicialización del esquema de PostgreSQL y siembra de datos iniciales (RI-03).
/// </summary>
public static class DatabaseInitializationExtensions {
    /// <summary>
    /// En desarrollo recrea la BD desde cero, aplica migraciones y siembra todos los perfiles.
    /// En producción aplica solo las migraciones pendientes sin destruir datos.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app) {
        Log.Information("Inicializando base de datos...");

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContextUsuarios>();

        if (app.Environment.IsDevelopment()) {
            Log.Warning("[DESARROLLO] Recreando base de datos desde cero...");
            await db.Database.EnsureDeletedAsync();
        }

        await db.Database.MigrateAsync();

        // En producción solo crea roles y, si se inyectan credenciales de admin, la cuenta Admin.
        await IdentitySeeder.SeedAsync(app.Services, app.Configuration);

        Log.Information("Base de datos inicializada correctamente.");
    }
}
