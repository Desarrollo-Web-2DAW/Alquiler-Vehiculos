using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Storage;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Repositories.Storage;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Storage;

/// <summary>
/// Gestiona los archivos en el disco del servidor y las rutas utilizadas para localizarlos.
/// Comprueba los archivos recibidos antes de guardarlos y devuelve los fallos mediante Result.
/// </summary>
///
public class StorageService : IStorageService
{
    private readonly string _rootPath;
    private readonly string _uploadPath;
    private readonly ILogger<StorageService> _logger;

    /// <summary>
    /// Lee los límites y tipos admitidos, calcula la ubicación del almacenamiento
    /// y crea su carpeta si todavía no existe, sin borrar su contenido.
    /// </summary>
    /// <param name="settings">Configuración que contiene la sección Storage.</param>
    /// <param name="logger">Registrador de los mensajes y errores de este servicio.</param>
    public StorageService(StorageSettings settings, ILogger<StorageService> logger
    )
    {
        _logger = logger;
        _uploadPath = settings.UploadPath;
        var webRoot = settings.CustomRootPath;
        _rootPath = Path.Combine(webRoot, _uploadPath);

        if (!Directory.Exists(_rootPath)) Directory.CreateDirectory(_rootPath);
        
        _logger.LogInformation("Storage service inicializado en: {Path}", _rootPath);
    }

    /// <inheritdoc cref="IStorageService.SaveFileAsync"/>
    public async Task<Result<Foto, DomainError>> SaveFileAsync(IFormFile file, string folder, string fileName)
    {
        try
        {
            var mainFolderPath = Path.Combine(_rootPath, folder);
            if (!Directory.Exists(mainFolderPath)) Directory.CreateDirectory(mainFolderPath);

            var filePath = Path.Combine(mainFolderPath, fileName);
            var relativePath = GetRelativePath(fileName, folder);
            
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            
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
            _logger.LogError(e, "Error guardando archivo");
            return await Task.FromResult(Result.Failure<Foto, DomainError>(StorageError.ErrorGuardando()));
        }
    }

    /// <inheritdoc cref="IStorageService.DescartarFotoAsync" />
    public async Task<Result<bool, DomainError>> DescartarFotoAsync(string nombre) {
        try {
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Contains("..") ||
                nombre.Contains('/') || nombre.Contains('\\') || Path.IsPathRooted(nombre)) {
                _logger.LogError("No se puede descartar la foto: el identificador no es un nombre válido.");
                return ValidationError.Create("El identificador debe ser un nombre de foto, no una ruta.");
            }

            if (await _fotoRepository.SoftDeleteSiNoAsociada(nombre)) return true;
            var foto = await _fotoRepository.GetById(nombre);
            if (foto is null || !foto.Activo || foto.DeletedAt is not null) return true;

            // Puede haberse confirmado el vehículo antes de fallar una operación posterior (p. ej., caché).
            _logger.LogWarning("Se conserva la foto {Nombre}: está asociada a un vehículo.", nombre);
            return new ConflictError("FOTO_ASOCIADA", "No se descarta una foto asociada a un vehículo.");
        } catch (Exception e) {
            _logger.LogError(e, "No se pudo descartar la foto {Nombre}; requiere revisión.", nombre);
            return StorageError.ErrorEliminando();
        }
    }

    /// <inheritdoc cref="IStorageService.DeleteFileAsync"/>
    public async Task<Result<bool, DomainError>> DeleteFileAsync(string filename)
    {
        if (string.IsNullOrEmpty(filename)) return await Task.FromResult(Result.Success<bool, DomainError>(true));

        try
        {
            var fullPath = GetFullPath(filename);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Archivo eliminado: {Filename}", filename);
            }

            return await Task.FromResult(Result.Success<bool, DomainError>(true));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error eliminando archivo {Filename}", filename);
            return await Task.FromResult(Result.Failure<bool, DomainError>(StorageError.ErrorEliminando()));
        }
    }

    /// <inheritdoc cref="IStorageService.FileExists"/>
    public bool FileExists(string filename)
    {
        if (string.IsNullOrEmpty(filename)) return false;
        return File.Exists(GetFullPath(filename));
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

        if (filename.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) filename = filename[prefix.Length..];
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
        return file is null or { Length: 0 };
    }

    /// <summary>
    /// Revisa que el archivo tenga contenido, no supere el tamaño permitido
    /// y cumpla las comprobaciones de extensión, tipo de contenido y nombre.
    /// </summary>
    /// <param name="file">Archivo que se comprueba antes de escribirlo en disco.</param>
    /// <returns>Éxito sin un valor asociado, o el primer error detectado.</returns>
    private UnitResult<DomainError> ValidateFile(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return UnitResult.Failure<DomainError>(ValidationError.Create("Debe proporcionar una imagen con contenido."));
        if (file.Length > _maxFileSize) return UnitResult.Failure<DomainError>(StorageError.ArchivoMuyGrande());

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
            return UnitResult.Failure<DomainError>(StorageError.ExtensionNoPermitida());

        var contentType = file.ContentType?.ToLowerInvariant();
        if (contentType is null || !_allowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            return UnitResult.Failure<DomainError>(StorageError.TipoContenidoNoPermitido());

        var filename = Path.GetFileName(file.FileName);
        if (filename.Contains("..") || filename.Contains('/') || filename.Contains('\\'))
            return UnitResult.Failure<DomainError>(StorageError.NombreArchivoInvalido());

        return UnitResult.Success<DomainError>();
    }
}
