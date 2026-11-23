using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Storage;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Repositories.Storage;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Storage;

/// <summary>Conserva los ficheros físicos aunque se dé de baja su entrada del catálogo.</summary>
public class FotoService(IFotoRepository repository, IStorageService storage) : IFotoService {
    public async Task<Result<Foto, DomainError>> Create(IFormFile? archivo) {
        if (archivo is null || storage.IsEmpty(archivo)) {
            return ValidationError.WithFieldErrors(new Dictionary<string, string[]> {
                ["archivo"] = ["Debe proporcionar una imagen con contenido."]
            });
        }
        var fotoRuta = await storage.SaveFileAsync(archivo, "vehiculos");
        if (fotoRuta.IsFailure) return fotoRuta.Error;
        var foto = await repository.GetById(fotoRuta.Value);
        if (foto == null) return FotoError.FotoNotFound(fotoRuta.Value);
        return foto;
    }

    public async Task<IEnumerable<Foto>> GetAll() {
        var fotos = await repository.GetAll();
        return fotos.Where(f => f.Activo && f.DeletedAt is null);
    }

    public async Task<Result<Foto, DomainError>> GetById(string nombre) {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Contains("..") ||
            nombre.Contains('/') || nombre.Contains('\\') || Path.IsPathRooted(nombre)) {
            return ValidationError.Create("El identificador debe ser un nombre de foto, no una ruta.");
        }

        var foto = await repository.GetById(nombre);
        if (foto is null || !foto.Activo || foto.DeletedAt is not null)
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
}
