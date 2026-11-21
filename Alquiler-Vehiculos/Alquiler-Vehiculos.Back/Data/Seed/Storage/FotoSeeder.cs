using Alquiler_Vehiculos.Data.Vehiculo;
using Alquiler_Vehiculos.Models.Storage;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Alquiler_Vehiculos.Data.Seed.Storage;

/// <summary>Datos iniciales de las fotos del catálogo, sin modificar archivos físicos.</summary>
public static class FotoSeeder {
    
    private static readonly string[] Matriculas = [
        "1234BCD", "2345CDF", "3456DFG", "4567FGH", "5678GHJ", "6789HJK",
        "7890JKL", "8901KLM", "9012MNP", "0123NPR", "1357PRS", "2468RST"
    ];

    /// <summary>
    /// Inserta las fotos solo si la tabla está vacía, sin borrar datos existentes.
    /// Requiere el esquema creado y las imágenes preparadas previamente en uploads/vehiculos.
    /// </summary>
    public static async Task SeedAsync(PostgreDbContext context,
        IConfiguration configuration, IWebHostEnvironment env) {
        if (await context.Fotos.IgnoreQueryFilters().AnyAsync()) {
            Log.Information("Fotos: ya existen datos, no se ejecuta la semilla.");
            return;
        }

        var fotos = BuildFotos(configuration, env);
        context.Fotos.AddRange(fotos);
        await context.SaveChangesAsync();
        Log.Information("Insertadas {Count} fotos de semilla.", fotos.Length);
    }

    private static Foto[] BuildFotos(IConfiguration configuration, IWebHostEnvironment env) {
        var uploadPath = configuration["Storage:UploadPath"] ?? "uploads";
        var rootPath = env.WebRootPath ?? env.ContentRootPath;
        var fecha = DateTime.UtcNow;
        var fotos = new List<Foto>();

        // Las imágenes las prepara el almacenamiento, no este seeder.
        // Solo se lee su tamaño real para no inventar los metadatos de Foto.
        foreach (var matricula in Matriculas) {
            var nombre = $"seed-{matricula}.jpg";
            var archivo = new FileInfo(Path.Combine(rootPath, uploadPath, "vehiculos", nombre));
            if (!archivo.Exists || archivo.Length == 0) {
                throw new InvalidOperationException($"Falta la imagen de semilla {nombre} o está vacía. Prepara primero el almacenamiento.");
            }

            fotos.Add(new Foto {
                Nombre = nombre,
                Ruta = $"/{uploadPath}/vehiculos/{nombre}",
                Extension = ".jpg",
                Tamano = archivo.Length,
                Activo = true,
                CreatedAt = fecha,
                UpdatedAt = null,
                DeletedAt = null
            });
        }

        return fotos.ToArray();
    }
}