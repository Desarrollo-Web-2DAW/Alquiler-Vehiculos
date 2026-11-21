using Alquiler_Vehiculos.Data.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace Alquiler_Vehiculos.Data.Seed.Vehiculo;

/// <summary>Datos iniciales de marcas y modelos disponibles del catálogo.</summary>
public static class MarcaSeeder {
    
    /// <summary>Inserta las marcas solo si la colección está vacía, sin borrar datos existentes.</summary>
    public static async Task SeedAsync(MongoDbContext context) {
        if (await context.Marcas.Find(FilterDefinition<MarcaModel>.Empty).AnyAsync()) {
            Log.Information("Marcas: ya existen datos, no se ejecuta la semilla.");
            return;
        }

        var marcas = BuildMarcas(DateTime.UtcNow);
        await context.Marcas.InsertManyAsync(marcas);
        Log.Information("Insertadas {Count} marcas de semilla.", marcas.Length);
    }

    internal static MarcaModel[] BuildMarcas(DateTime fecha) {

        // Identificadores estables de la semilla. VehiculoSeeder consulta las marcas por nombre.
        return [
            new MarcaModel(ObjectId.Parse("64b000000000000000000001"), "Toyota", "Japón",
                "https://www.toyota.es", ["Corolla Sedán"], false, fecha, null, null),
            new MarcaModel(ObjectId.Parse("64b000000000000000000002"), "Renault", "Francia",
                "https://www.renault.es", ["Megane", "Grand Scénic"], false, fecha, null, null),
            new MarcaModel(ObjectId.Parse("64b000000000000000000003"), "Nissan", "Japón",
                "https://www.nissan.es", ["Ariya"], false, fecha, null, null),
            new MarcaModel(ObjectId.Parse("64b000000000000000000004"), "Jeep", "Estados Unidos",
                "https://www.jeep.es", ["Wrangler"], false, fecha, null, null),
            new MarcaModel(ObjectId.Parse("64b000000000000000000005"), "Volkswagen", "Alemania",
                "https://www.volkswagen.es", ["Passat Variant"], false, fecha, null, null),
            new MarcaModel(ObjectId.Parse("64b000000000000000000006"), "BMW", "Alemania",
                "https://www.bmw.es", ["Serie 4 Coupé"], false, fecha, null, null),
            new MarcaModel(ObjectId.Parse("64b000000000000000000007"), "MINI", "Reino Unido",
                "https://www.mini.es", ["Cabrio"], true, fecha.AddMonths(-6), fecha, fecha),
            new MarcaModel(ObjectId.Parse("64b000000000000000000008"), "Ford", "Estados Unidos",
                "https://www.ford.es", ["Transit"], false, fecha, null, null)
        ];
    }
}
