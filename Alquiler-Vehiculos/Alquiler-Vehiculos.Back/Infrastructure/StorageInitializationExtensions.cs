using Alquiler_Vehiculos.Config;
using Serilog;
using Path = System.IO.Path;

namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
/// Extensiones para preparar el directorio de almacenamiento de imágenes.
/// </summary>
public static class StorageInitializationExtensions {

    /// <summary>
    /// Prepara la carpeta de subidas y, en desarrollo, copia desde <c>SeedImages</c>
    /// las imágenes que todavía no existan. No borra los archivos almacenados para
    /// conservar el histórico de fotografías (RF-48).
    /// </summary>
    /// <param name="app">Aplicación de la que se obtienen las rutas del entorno.</param>
    /// <param name="configuration">Configuración de la app.</param>
    /// <param name="isDevelopment">Indica si se copian las imágenes iniciales.</param>
    public static void InitializeStorage(this WebApplication app, IConfiguration configuration, bool isDevelopment) {
        var storageSettings = configuration
            .GetSection(StorageSettings.SectionName)
            .Get<StorageSettings>() ?? new StorageSettings();
        var uploadPath = string.IsNullOrWhiteSpace(storageSettings.UploadPath)
            ? "uploads"
            : storageSettings.UploadPath;

        var rootPath = app.Environment.WebRootPath ?? app.Environment.ContentRootPath;
        var storagePath = Path.Combine(rootPath, uploadPath);
        var storageDirectory = new DirectoryInfo(storagePath);

        try {
            Log.Information("Preparando directorio de almacenamiento: {Path}", storagePath);

            // Si la carpeta no existe, la creamos y añadimos un .gitkeep.
            if (!storageDirectory.Exists) {
                Directory.CreateDirectory(storagePath);
                File.WriteAllText(Path.Combine(storagePath, ".gitkeep"), string.Empty);
                Log.Information("Se ha creado el directorio {Carpeta} y el archivo .gitkeep.", uploadPath);
            }

            if (!isDevelopment) {
                Log.Information("Almacenamiento: sin copia de SeedImages fuera de Development.");
                return;
            }

            var sourceSeedPath = Path.Combine(app.Environment.ContentRootPath, "SeedImages");
            if (!Directory.Exists(sourceSeedPath)) {
                Log.Warning("La carpeta SeedImages no existe; se omite el sembrado inicial de imágenes.");
                return;
            }

            foreach (var (key, domainFolderName) in storageSettings.Folders) {
                var targetDirectory = Path.Combine(storagePath, domainFolderName);
                Directory.CreateDirectory(targetDirectory);

                var subGitKeep = Path.Combine(targetDirectory, ".gitkeep");
                if (!File.Exists(subGitKeep)) {
                    File.WriteAllText(subGitKeep, string.Empty);
                }

                Log.Information("Carpeta preparada -> {Nombre}: {Path}", key, targetDirectory);

                var seedDirectory = Path.Combine(sourceSeedPath, domainFolderName);
                if (!Directory.Exists(seedDirectory)) {
                    Log.Warning("No hay imágenes iniciales para {Nombre} en {Path}.", key, seedDirectory);
                    continue;
                }

                CopyDirectoryContents(seedDirectory, targetDirectory);
                Log.Information("Archivos iniciales preparados para {Nombre} desde {Source}", key, seedDirectory);
            }
        } catch (Exception e) {
            Log.Error(e, "Ha ocurrido un error al preparar el almacenamiento de imágenes.");
        }
    }

    /// <summary>
    /// Copia recursivamente los archivos que faltan, sin sobrescribir los ya
    /// almacenados ni eliminar subidas anteriores.
    /// </summary>
    private static void CopyDirectoryContents(string sourceDir, string targetDir) {
        foreach (var file in Directory.GetFiles(sourceDir)) {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(targetDir, fileName);
            if (!File.Exists(destFile)) File.Copy(file, destFile);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir)) {
            var dirName = Path.GetFileName(subDir);
            var destSubDir = Path.Combine(targetDir, dirName);
            Directory.CreateDirectory(destSubDir);
            CopyDirectoryContents(subDir, destSubDir);
        }
    }
}
