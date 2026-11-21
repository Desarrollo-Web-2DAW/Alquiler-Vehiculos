using Alquiler_Vehiculos.Data.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Alquiler_Vehiculos.Data.Seed.Vehiculo;

/// <summary>Datos iniciales de concesionarios ficticios para desarrollo.</summary>
public static class ConcesionarioSeeder {
    
    /// <summary>
    /// Inserta los concesionarios solo si la tabla está vacía, sin borrar datos existentes.
    /// El esquema de PostgreSQL debe estar creado antes de llamar a este método.
    /// </summary>
    public static async Task SeedAsync(PostgreDbContext context) {
        if (await context.Concesionarios.IgnoreQueryFilters().AnyAsync()) {
            Log.Information("Concesionarios: ya existen datos, no se ejecuta la semilla.");
            return;
        }

        var concesionarios = BuildConcesionarios(DateTime.UtcNow);
        context.Concesionarios.AddRange(concesionarios);
        await context.SaveChangesAsync();
        Log.Information("Insertados {Count} concesionarios de semilla.", concesionarios.Length);
    }

    internal static ConcesionarioModel[] BuildConcesionarios(DateTime fecha) {

        // Id = 0: PostgreSQL genera el identificador de cada concesionario.
        // Los nombres y los datos de contacto son ficticios.
        return [
            new ConcesionarioModel(0, "Motor Leganés", "Leganés", "910000001",
                "contacto@motorleganes.example", false, fecha, null, null),
            new ConcesionarioModel(0, "Automóviles Madrid Centro", "Madrid", "910000002",
                "contacto@madridcentro.example", false, fecha, null, null),
            new ConcesionarioModel(0, "Motor Getafe", "Getafe", "910000003",
                "contacto@motorgetafe.example", false, fecha, null, null),
            new ConcesionarioModel(0, "Vehículos Alcorcón", "Alcorcón", "910000004",
                "contacto@vehiculosalcorcon.example", false, fecha, null, null),
            new ConcesionarioModel(0, "Motor Fuenlabrada", "Fuenlabrada", "910000005",
                "contacto@motorfuenlabrada.example", true, fecha.AddMonths(-6), fecha, fecha)
        ];
    }
}
