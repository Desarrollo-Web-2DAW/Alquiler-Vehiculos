using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Storage;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Repositories.Storage;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;

namespace Alquiler_Vehiculos.Services.Storage;

/// <summary>
/// Gestiona los archivos en el disco del servidor y las rutas utilizadas para localizarlos.
/// Comprueba los archivos recibidos antes de guardarlos y devuelve los fallos mediante Result.
/// </summary>
public class StorageService : IStorageService
{
    private readonly StorageSettings _settings;
    private readonly string _rootPath;
    private readonly string _uploadPath;
    private readonly ILogger<StorageService> _logger;

    /// <summary>
    /// Lee los límites y tipos admitidos, calcula la ubicación del almacenamiento
    /// y crea su carpeta si todavía no existe, sin borrar su contenido.
    /// </summary>
    /// <param name="settings">Configuración que contiene la sección Storage.</param>
    /// <param name="logger">Registrador de los mensajes y errores de este servicio.</param>
    public StorageService(IOptions<StorageSettings> settings, ILogger<StorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _uploadPath = _settings.UploadPath;
        
        var webRoot = string.IsNullOrEmpty(_settings.CustomRootPath) 
            ? AppContext.BaseDirectory 
            : _settings.CustomRootPath;
            
        _rootPath = Path.Combine(webRoot, _uploadPath);

        if (!Directory.Exists(_rootPath))
        {
            Directory.CreateDirectory(_rootPath);
            _logger.LogInformation("Directorio raíz de almacenamiento creado en: {Path}", _rootPath);
        }
        
        _logger.LogInformation("Storage service inicializado correctamente en: {Path}", _rootPath);
    }

    /// <inheritdoc cref="IStorageService.SaveFileAsync"/>
    public async Task<Result<Foto, DomainError>> SaveFileAsync(
        IFormFile file, string folder, string fileName, CancellationToken ct = default)
    {
        try
        {
            var mainFolderPath = Path.Combine(_rootPath, folder);
            if (!Directory.Exists(mainFolderPath))
            {
                Directory.CreateDirectory(mainFolderPath);
                _logger.LogInformation("Carpeta de categoría creada: {FolderPath}", mainFolderPath);
            }

            var filePath = Path.Combine(mainFolderPath, fileName);
            var relativePath = GetRelativePath(fileName, folder);
            
            _logger.LogInformation("Iniciando guardado de archivo: {FileName} en ruta: {FilePath}", fileName, filePath);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }
            
            _logger.LogInformation("Archivo guardado exitosamente: {FileName} ({Size} bytes)", fileName, file.Length);

            var fotoModel = new Foto
            {
                Nombre = fileName,
                Ruta = relativePath,
                Extension = Path.GetExtension(file.FileName).ToLowerInvariant(),
                Tamano = file.Length,
                Activo = true,             
                CreatedAt = DateTime.UtcNow
            };

            return fotoModel;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error crítico al guardar el archivo {FileName} en la carpeta {Folder}", fileName, folder);
            return StorageError.ErrorGuardando();
        }
    }

    /// <inheritdoc cref="IStorageService.DeleteFileAsync"/>
    public async Task<Result<bool, DomainError>> DeleteFileAsync(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            _logger.LogWarning("Se intentó eliminar un archivo con nombre nulo o vacío.");
            return true;
        }

        try
        {
            var fullPath = GetFullPath(filename);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Archivo eliminado físicamente del servidor: {Filename} (Ruta: {FullPath})", filename, fullPath);
            }
            else
            {
                _logger.LogWarning("Se intentó eliminar el archivo {Filename}, pero no se encontró en la ruta: {FullPath}", filename, fullPath);
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al intentar eliminar el archivo {Filename}", filename);
            return StorageError.ErrorEliminando();
        }
    }

    /// <inheritdoc cref="IStorageService.FileExists"/>
    public bool FileExists(string filename)
    {
        if (string.IsNullOrEmpty(filename)) return false;
        
        var exists = File.Exists(GetFullPath(filename));
        _logger.LogDebug("Comprobando existencia del archivo {Filename}: {Exists}", filename, exists);
        return exists;
    }

    /// <inheritdoc cref="IStorageService.GetFullPath"/>
    /// <remarks>
    /// Devuelve sin cambios las rutas que Path.IsPathRooted reconoce como enraizadas.
    /// Para las demás, retira el prefijo de uploads si aparece y las combina con la raíz del almacenamiento.
    /// </remarks>
    public string GetFullPath(string filename)
    {
        if (Path.IsPathRooted(filename)) return filename;
        var prefix = $"/{_uploadPath}/";

        if (filename.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) 
            filename = filename[prefix.Length..];
            
        return Path.Combine(_rootPath, filename);
    }

    /// <inheritdoc cref="IStorageService.GetRelativePath"/>
    public string GetRelativePath(string filename, string folder = "vehiculos")
    {
        return $"/{_uploadPath}/{folder}/{filename}";
    }

    /// <inheritdoc cref="IStorageService.IsEmpty"/>
    public bool IsEmpty(IFormFile? file)
    {
        var isEmpty = file is null or { Length: 0 };
        if (isEmpty)
        {
            _logger.LogDebug("Se evaluó un archivo y se determinó que está vacío o es nulo.");
        }
        return isEmpty;
    }
}