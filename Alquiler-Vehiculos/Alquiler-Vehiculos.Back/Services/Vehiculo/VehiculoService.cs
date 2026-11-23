using Alquiler_Vehiculos.Cache;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Storage;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Mappers.Vehiculo;
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;
using Alquiler_Vehiculos.Repositories.Vehiculo.Concesionario;
using Alquiler_Vehiculos.Services.Vehiculo.Transactions;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Vehiculo;
using CSharpFunctionalExtensions;
using Serilog;

namespace Alquiler_Vehiculos.Services.Vehiculo;

/// <summary>
/// Servicio de aplicación de Vehículo: validación, caché y orquestación.
/// Las operaciones que tocan más de una entidad delegan en
/// <see cref="IVehiculoTransactionsService"/>.
/// </summary>
public class VehiculoService(
    IVehiculoRepository repository,
    IFotoRepository fotoRepository,
    IVehiculoTransactionsService transactionsService,
    ICacheService cache,
    CreateVehiculoValidator createVehiculoValidator,
    UpdateVehiculoValidator updateVehiculoValidator,
    IConcesionarioRepository concesionarioRepository
    ) : IVehiculoService
{
    private const string ClaveVehiculo = "vehiculo:id:";
    private const string ClaveMatricula = "vehiculo:matricula:";

    /// <inheritdoc cref="IVehiculoService.GetAll" />
    public async Task<IEnumerable<VehiculoDto>> GetAll()
    {
        Log.Debug("Consultando todos los vehículos");
        var lista = await repository.GetAll();
        return lista.Select(v => v.ToDto());
    }

    /// <inheritdoc cref="IVehiculoService.GetPaged" />
    public async Task<(IEnumerable<VehiculoDto> Items, int TotalItems)> GetPaged(int page, int pageSize)
    {
        Log.Debug("Consultando vehículos página {Page} con tamaño {PageSize}", page, pageSize);
        var (items, totalItems) = await repository.GetPaged(page, pageSize);
        return (items.Select(v => v.ToDto()), totalItems);
    }

    /// <inheritdoc cref="IVehiculoService.GetById" />
    public async Task<Result<VehiculoDto, DomainError>> GetById(string key)
    {
        Log.Debug("Consultando vehículo con ID {Id}", key);

        var cached = await cache.GetAsync<VehiculoDto>(ClaveVehiculo + key);
        if (cached is not null) return Result.Success<VehiculoDto, DomainError>(cached);

        var vehiculo = await repository.GetById(key);
        if (vehiculo is null)
        {
            return Result.Failure<VehiculoDto, DomainError>(VehiculoError.NotFoundVehiculo(key));
        }

        var dto = vehiculo.ToDto();
        await cache.SetAsync(ClaveVehiculo + key, dto);
        await cache.SetAsync(ClaveMatricula + vehiculo.Matrícula, dto);

        return Result.Success<VehiculoDto, DomainError>(dto);
    }

    /// <inheritdoc cref="IVehiculoService.Create" />
    public async Task<Result<VehiculoDto, DomainError>> Create(CreateVehiculoDto item)
    {
        Log.Debug("Creando vehículo con matrícula {Matricula}", item.Matricula);
        
        var validationResult = await createVehiculoValidator.ValidateAsync(item);
        if (!validationResult.IsValid)
        {
            Log.Information("El vehículo contiene datos no válidos");
            var errorsDictionary = new Dictionary<string, string[]>(validationResult.ToDictionary());
            return ValidationError.WithFieldErrors(errorsDictionary);
        }

        if (await repository.ExistByMatricula(item.Matricula))
        {
            return Result.Failure<VehiculoDto, DomainError>(VehiculoError.VehiculoYaExiste(item.Matricula));
        }

        if (!await concesionarioRepository.ExistById(item.Concesionario))
            return ValidationError.Create($"El concesionario con ID '{item.Concesionario}' no existe.");

        var foto= await fotoRepository.GetById(item.Foto);
        if (foto is null) return FotoError.FotoNotFound(item.Foto);

        var vehiculo = item.ToVehiculo(foto);
        if (vehiculo.IsFailure) return vehiculo.Error;

        var creado = await repository.Create(vehiculo.Value);
        if (creado.IsFailure) return creado.Error;
        var dto = creado.Value.ToDto();
        await cache.RemoveAsync(ClaveVehiculo + creado.Value.Id);
        await cache.RemoveAsync(ClaveMatricula + creado.Value.Matrícula);
        await cache.SetAsync(ClaveVehiculo + creado.Value.Id, dto);
        await cache.SetAsync(ClaveMatricula + creado.Value.Matrícula, dto);
        return Result.Success<VehiculoDto, DomainError>(dto);
    }

    /// <inheritdoc cref="IVehiculoService.Update" />
    public async Task<Result<VehiculoDto, DomainError>> Update(UpdateVehiculoDto item, string key)
    {
        Log.Debug("Actualizando vehículo con ID {Id}", key);

        var validationResult = await updateVehiculoValidator.ValidateAsync(item);
        if (!validationResult.IsValid)
        {
            Log.Information("El vehículo contiene datos no válidos");
            var errorsDictionary = new Dictionary<string, string[]>(validationResult.ToDictionary());
            return ValidationError.WithFieldErrors(errorsDictionary);
        }

        var existente = await repository.GetById(key);
        if (existente is null)
        {
            return Result.Failure<VehiculoDto, DomainError>(VehiculoError.NotFoundVehiculo(key));
        }

        if (item.Matricula != existente.Matrícula && await repository.ExistByMatricula(item.Matricula))
        {
            return Result.Failure<VehiculoDto, DomainError>(VehiculoError.VehiculoYaExiste(item.Matricula));
        }

        if (!await concesionarioRepository.ExistById(item.Concesionario))
            return ValidationError.Create($"El concesionario con ID '{item.Concesionario}' no existe.");

        var foto = await fotoRepository.GetById(item.Foto);
        if (foto is null) return FotoError.FotoNotFound(item.Foto);

        var mapeadoResult = item.ToVehiculoModel(foto);
        if (mapeadoResult.IsFailure)
        {
            return Result.Failure<VehiculoDto, DomainError>(mapeadoResult.Error);
        }

        var mapeado = mapeadoResult.Value;
        
        var vehiculo = existente with
        {
            Matrícula = mapeado.Matrícula,
            Marca = mapeado.Marca,
            Modelo = mapeado.Modelo,
            Acriss = mapeado.Acriss,
            Carroceria = mapeado.Carroceria,
            Combustible = mapeado.Combustible,
            Transmision = mapeado.Transmision,
            Color = mapeado.Color,
            PrecioBaseDia = mapeado.PrecioBaseDia,
            Plazas = mapeado.Plazas,
            MaleteroLitros = mapeado.MaleteroLitros,
            AutonomiaKm = mapeado.AutonomiaKm,
            Kilometraje = mapeado.Kilometraje,
            FechaUltimaRevision = mapeado.FechaUltimaRevision,
            ConesionarioFk = mapeado.ConesionarioFk,
            UpdatedAt = DateTime.UtcNow,
            Foto = mapeado.Foto,
            FotoFk = mapeado.FotoFk
        };

        var result = await transactionsService.UpdateAsync(key, vehiculo);
        if (result.IsFailure)
        {
            return Result.Failure<VehiculoDto, DomainError>(result.Error);
        }

        await cache.RemoveAsync(ClaveVehiculo + key);
        await cache.RemoveAsync(ClaveMatricula + existente.Matrícula);
        if (vehiculo.Matrícula != existente.Matrícula)
            await cache.RemoveAsync(ClaveMatricula + vehiculo.Matrícula);

        var dto = result.Value.ToDto();
        await cache.SetAsync(ClaveVehiculo + key, dto);
        await cache.SetAsync(ClaveMatricula + vehiculo.Matrícula, dto);

        return Result.Success<VehiculoDto, DomainError>(dto);
    }

    /// <inheritdoc cref="IVehiculoService.SoftDelete" />
    public async Task<Result<bool, DomainError>> SoftDelete(string key)
    {
        Log.Debug("Borrado lógico de vehículo con ID {Id}", key);

        var result = await transactionsService.DeleteAsync(key);
        if (result.IsFailure)
        {
            return Result.Failure<bool, DomainError>(result.Error);
        }

        await cache.RemoveAsync(ClaveVehiculo + key);
        await cache.RemoveAsync(ClaveMatricula + result.Value.Matrícula);

        return Result.Success<bool, DomainError>(true);
    }

    /// <inheritdoc cref="IVehiculoService.Restore" />
    public async Task<Result<VehiculoDto, DomainError>> Restore(string key)
    {
        Log.Debug("Restaurando vehículo con ID {Id}", key);

        var result = await transactionsService.RestoreAsync(key);
        if (result.IsFailure)
        {
            return Result.Failure<VehiculoDto, DomainError>(result.Error);
        }

        var dto = result.Value.ToDto();
        await cache.RemoveAsync(ClaveVehiculo + key);
        await cache.RemoveAsync(ClaveMatricula + result.Value.Matrícula);
        await cache.SetAsync(ClaveVehiculo + key, dto);
        await cache.SetAsync(ClaveMatricula + result.Value.Matrícula, dto);

        return Result.Success<VehiculoDto, DomainError>(dto);
    }
}
