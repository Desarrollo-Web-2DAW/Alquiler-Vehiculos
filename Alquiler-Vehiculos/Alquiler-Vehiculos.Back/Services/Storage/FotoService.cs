using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Storage;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Validators.Storage;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;

namespace Alquiler_Vehiculos.Services.Storage;

/// <summary>
/// Conserva los ficheros físicos aunque se dé de baja su entrada del catálogo.
/// </summary>
public class FotoService(FileValidator validator, IFotoRepository repository, IStorageService storage, IOptions<StorageSettings> settingss) : IFotoService
{
    private StorageSettings _settings = settingss.Value;
    
    public async Task<Result<Foto, DomainError>> Create(IFormFile archivo, string folderName) {
        
        var validationResult = await validator.ValidateAsync(archivo);
        
        if (!validationResult.IsValid)
        {
            var errorsDictionary = new Dictionary<string, string[]>(validationResult.ToDictionary());
            return ValidationError.WithFieldErrors(errorsDictionary);
        }

        var fileName = GenerateUniqueFilename(archivo.FileName);

        var resultFotoFisica = await storage.SaveFileAsync(archivo, folderName, fileName);

        if (resultFotoFisica.IsFailure) return resultFotoFisica.Error;

        try
        {
            var result = await repository.Create(resultFotoFisica.Value);
            return result;
        }
        catch (Exception ex)
        {
            await storage.DeleteFileAsync(fileName);
            return FotoError.NosePudoCrear(fileName);
        }
    }

    public async Task<IEnumerable<Foto>> GetAll() {
        var fotos = await repository.GetAll();
        return fotos.Where(f => f.Activo && f.DeletedAt is null);
    }

    public async Task<Result<Foto, DomainError>> GetById(string nombre) {
        var result = await GetHistoricoById(nombre);
        if (result.IsFailure) return result.Error;
        if (!result.Value.Activo || result.Value.DeletedAt is not null) return FotoError.FotoNotFound(nombre);
        return result.Value;
    }

    public async Task<Result<Foto, DomainError>> GetHistoricoById(string nombre) {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Contains("..") ||
            nombre.Contains('/') || nombre.Contains('\\') || Path.IsPathRooted(nombre)) {
            return ValidationError.Create("El identificador debe ser un nombre de foto, no una ruta.");
        }

        var foto = await repository.GetById(nombre);
        if (foto is null)
            return FotoError.FotoNotFound(nombre);

        return foto;
    }

    public async Task<Result<bool, DomainError>> SoftDelete(string nombre) {
        var existente = await GetById(nombre);
        if (existente.IsFailure) return existente.Error;

        // RF-48: no llamar a DeleteFileAsync, que elimina el fichero físico.
        // Se conserva siempre, incluso cuando todavía no hay módulo de reservas.
        if (!await repository.SoftDelete(nombre)) return FotoError.FotoNotFound(nombre);
        return true;
    }
    
    /// <summary>
    /// Genera un nombre de archivo único combinando una marca de tiempo UTC y un GUID corto, 
    /// conservando únicamente la extensión original en minúsculas. Se descarta el nombre 
    /// original para evitar problemas de compatibilidad con caracteres especiales o de otros idiomas.
    /// </summary>
    /// <param name="fileName">Nombre original del archivo del cual se extraerá la extensión.</param>
    /// <returns>Un nombre de archivo único y seguro para contenedores y sistemas de almacenamiento.</returns>
    private static string GenerateUniqueFilename(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return $"{timestamp}_{uniqueId}{extension}";
    }
}
