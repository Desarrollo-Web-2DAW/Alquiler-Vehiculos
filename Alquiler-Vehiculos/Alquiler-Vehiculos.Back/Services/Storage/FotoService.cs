using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Storage;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Validators.Storage;
using CSharpFunctionalExtensions;
using Serilog;

namespace Alquiler_Vehiculos.Services.Storage;

/// <summary>
/// Conserva los ficheros físicos aunque se dé de baja su entrada del catálogo.
/// </summary>
public class FotoService(FileValidator validator, IFotoRepository repository, IStorageService storage) : IFotoService
{
    
    public async Task<Result<Foto, DomainError>> PrepareAsync(
        IFormFile archivo, string folderName = "vehiculos", CancellationToken ct = default) {
        var validationResult = await validator.ValidateAsync(archivo, ct);
        if (!validationResult.IsValid)
        {
            var errorsDictionary = new Dictionary<string, string[]>(validationResult.ToDictionary());
            return ValidationError.WithFieldErrors(errorsDictionary);
        }

        var fileName = GenerateUniqueFilename(archivo.FileName);
        return await storage.SaveFileAsync(archivo, folderName, fileName, ct);
    }

    public Task<Result<bool, DomainError>> DeletePreparedFileAsync(
        string nombre, string folderName = "vehiculos") =>
        storage.DeleteFileAsync(Path.Combine(folderName, nombre));

    public async Task<Result<Foto, DomainError>> Create(
        IFormFile archivo, string folderName = "vehiculos", CancellationToken ct = default) {
        var preparada = await PrepareAsync(archivo, folderName, ct);
        if (preparada.IsFailure) return preparada.Error;
        try
        {
            var result = await repository.Create(preparada.Value);
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo crear el registro de la foto {Nombre}", preparada.Value.Nombre);
            var borrado = await storage.DeleteFileAsync(Path.Combine(folderName, preparada.Value.Nombre));
            if (borrado.IsFailure)
                Log.Error("No se pudo eliminar el archivo de la foto fallida {Nombre}: {Error}",
                    preparada.Value.Nombre, borrado.Error);
            return FotoError.NosePudoCrear(preparada.Value.Nombre);
        }
    }

    public async Task<IEnumerable<Foto>> GetAll() {
        var fotos = await repository.GetAll();
        return fotos.Where(f => f.Activo && f.DeletedAt is null);
    }

    public async Task<Result<Foto, DomainError>> GetById(string nombre) {
        var foto = await repository.GetById(nombre);
        if (foto is null) return FotoError.FotoNotFound(nombre);
        return foto;
    }

    public Task<Result<Foto, DomainError>> GetHistoricoById(string nombre) => GetById(nombre);
    
    public async Task<Result<bool, DomainError>> SoftDelete(string nombre) {
        var existente = await GetById(nombre);
        if (existente.IsFailure) return existente.Error;
        if (await repository.IsReferencedByVehicle(nombre))
            return FotoError.FotoReferenciada(nombre);
        if (!await repository.SoftDeleteSiNoAsociada(nombre)) {
            if (await repository.IsReferencedByVehicle(nombre))
                return FotoError.FotoReferenciada(nombre);
            return FotoError.FotoNotFound(nombre);
        }
        return true;
    }
    
    public async Task<Result<bool, DomainError>> HardDelete(string nombre) {
        var existente = await GetById(nombre);
        if (existente.IsFailure) return existente.Error;
        if (await repository.IsReferencedByVehicle(nombre))
            return FotoError.FotoReferenciada(nombre);
        
        var resultRepository = await repository.HardDelete(nombre);
        if (!resultRepository) return FotoError.NoSePudoDarDeBaja(nombre);
        
        var result = await storage.DeleteFileAsync(Path.Combine("vehiculos", nombre));
        return result.IsFailure? result.Error : result.Value;
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
