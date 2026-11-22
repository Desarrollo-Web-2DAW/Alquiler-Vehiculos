using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Storage;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Storage;

/// <summary>Subida, consulta y baja lógica de fotografías del catálogo (RF-48).</summary>
public interface IFotoService {
    Task<Result<Foto, DomainError>> Create(IFormFile? archivo);
    Task<IEnumerable<Foto>> GetAll();
    Task<Result<Foto, DomainError>> GetById(string nombre);
    Task<Result<bool, DomainError>> SoftDelete(string nombre);
}
