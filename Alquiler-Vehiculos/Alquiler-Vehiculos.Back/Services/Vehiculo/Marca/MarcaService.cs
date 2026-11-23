using Alquiler_Vehiculos.Cache;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Marca;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Mappers.Vehiculo;
using Alquiler_Vehiculos.Repositories.Vehiculo.Marca;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Marca;
using CSharpFunctionalExtensions;
using MongoDB.Bson;
using Serilog;

namespace Alquiler_Vehiculos.Services.Vehiculo.Marca;

/// <summary>Validación, mapeo y caché del catálogo de marcas (RF-09).</summary>
public class MarcaService(
    IMarcaRepository repository,
    ICacheService cache,
    CreateMarcaValidator createMarcaValidator,
    UpdateMarcaValidator updateMarcaValidator) : IMarcaService {
    private const string ClaveMarca = "marca:";

    /// <inheritdoc cref="IMarcaService.GetAll" />
    public async Task<IEnumerable<MarcaDto>> GetAll() {
        Log.Debug("Consultando todas las marcas activas");
        var marcas = await repository.GetAll();
        return marcas.Select(m => m.ToDto());
    }

    /// <inheritdoc cref="IMarcaService.GetById" />
    public async Task<Result<MarcaDto, DomainError>> GetById(string key) {
        if (!ObjectId.TryParse(key, out var id)) return MarcaError.NotFoundMarca(key);
        key = id.ToString();
        Log.Debug("Consultando marca con ID {Id}", key);
        var cached = await cache.GetAsync<MarcaDto>(ClaveMarca + key);
        if (cached is not null) return cached;

        var marca = await repository.GetById(key);
        if (marca is null) return MarcaError.NotFoundMarca(key);

        var dto = marca.ToDto();
        await cache.SetAsync(ClaveMarca + key, dto);
        return dto;
    }

    /// <inheritdoc cref="IMarcaService.Create" />
    public async Task<Result<MarcaDto, DomainError>> Create(CreateMarcaDto item) {
        Log.Debug("Creando marca con nombre {Nombre}", item.Nombre);
        var validationResult = await createMarcaValidator.ValidateAsync(item);
        if (!validationResult.IsValid) {
            return ValidationError.WithFieldErrors(new Dictionary<string, string[]>(validationResult.ToDictionary()));
        }

        var creada = await repository.Create(item.ToMarca());
        await cache.RemoveAsync(ClaveMarca + creada.Id);
        return creada.ToDto();
    }

    /// <inheritdoc cref="IMarcaService.Update" />
    public async Task<Result<MarcaDto, DomainError>> Update(UpdateMarcaDto item, string key) {
        Log.Debug("Actualizando marca con ID {Id}", key);
        var validationResult = await updateMarcaValidator.ValidateAsync(item);
        if (!validationResult.IsValid) {
            return ValidationError.WithFieldErrors(new Dictionary<string, string[]>(validationResult.ToDictionary()));
        }

        if (!ObjectId.TryParse(key, out var id)) return MarcaError.NotFoundMarca(key);
        key = id.ToString();
        var existente = await repository.GetById(key);
        if (existente is null) return MarcaError.NotFoundMarca(key);

        // Se conserva el identificador y la auditoría. Los modelos se sustituyen, no se acumulan.
        var marca = existente with {
            Nombre = item.Nombre,
            Pais = item.Pais,
            Web = item.Web,
            ModelosDisponibles = new List<string>(item.ModelosDisponibles)
        };
        var actualizada = await repository.Update(key, marca);
        if (actualizada is null) return MarcaError.NotFoundMarca(key);

        await cache.RemoveAsync(ClaveMarca + key);
        return actualizada.ToDto();
    }

    /// <inheritdoc cref="IMarcaService.SoftDelete" />
    public async Task<Result<bool, DomainError>> SoftDelete(string key) {
        if (!ObjectId.TryParse(key, out var id)) return MarcaError.NotFoundMarca(key);
        key = id.ToString();
        Log.Debug("Baja lógica de marca con ID {Id}", key);
        if (!await repository.ExistById(key)) return MarcaError.NotFoundMarca(key);
        if (!await repository.SoftDelete(key)) return MarcaError.BorradoError();

        // RF-09: no se bloquea por tener vehículos ni se realiza borrado en cascada.
        await cache.RemoveAsync(ClaveMarca + key);
        return true;
    }
}
