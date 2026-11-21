using Alquiler_Vehiculos.Cache;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Storage;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Mappers.Vehiculo;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Vehiculo;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Alquiler_Vehiculos.Services.Vehiculo;

public class VehiculoService(
    IVehiculoRepository repository,
    IFotoRepository fotoRepository,
    VehiculoTransactionsService transactionsService,
    ICacheService cache,
    CreateVehiculoValidator createVehiculoValidator,
    UpdateVehiculoValidator updateVehiculoValidator) : IVehiculoService
{
    private string _vehiculoKey => "vehiculo:";

    /// <inheritdoc cref="IVehiculoService.GetAll" />
    public async Task<IEnumerable<VehiculoDto>> GetAll() {
        Log.Debug("Consultando todos los vehículos");
        var lista = await repository.GetAll();
        return lista.Select(v => v.ToDto());
    }

    /// <inheritdoc cref="IVehiculoService.GetById" />
    public async Task<Result<VehiculoDto, DomainError>> GetById(string key) {
        Log.Debug("Consultando vehículo con ID {Id}", key);
        var cached = await cache.GetAsync<VehiculoDto>(_vehiculoKey + key);
        if (cached != null) return Result.Success<VehiculoDto, DomainError>(cached);
        
        var vehiculo = await repository.GetById(key);
        if (vehiculo == null) return Result.Failure<VehiculoDto, DomainError>(VehiculoError.NotFoundVehiculo(key));

        var dto = vehiculo.ToDto();
        await cache.SetAsync(_vehiculoKey + key, dto);
        await cache.SetAsync(_vehiculoKey + vehiculo.Matrícula, dto);
        
        return Result.Success<VehiculoDto, DomainError>(dto);
    }
    
    /// <inheritdoc cref="IVehiculoService.Create" />
    public async Task<Result<VehiculoDto, DomainError>> Create(CreateVehiculoDto item) {
        Log.Debug("Creando vehículo con matrícula {Matricula}", item.Matricula);

        var validationResult = await createVehiculoValidator.ValidateAsync(item);
        
        if (!validationResult.IsValid) {
            Log.Information("El vehículo contiene datos no válidos");
            var errorsDictionary = new Dictionary<string, string[]>(validationResult.ToDictionary());
            return BadRequestError.ValidationError.WithFieldErrors(errorsDictionary);
        }
        
        if (await repository.ExistByMatricula(item.Matricula)) {
            return Result.Failure<VehiculoDto, DomainError>(VehiculoError.VehiculoYaExiste(item.Matricula));
        }

        var foto = await fotoRepository.GetById(item.Foto);

        if (foto == null) return FotoError.FotoNotFound(item.Foto);

        var vehiculo = item.ToVehiculo(foto);
        if (vehiculo.IsFailure) return vehiculo.Error;
        
        var creado = await repository.Create(vehiculo.Value);
        
        var dto = creado.ToDto();

        return Result.Success<VehiculoDto, DomainError>(dto);
    }

    /// <inheritdoc cref="DbLoggerCategory.Update" />
    public async Task<Result<VehiculoDto, DomainError>> Update(UpdateVehiculoDto item, string key) {
        Log.Debug("Actualizando vehículo con ID {Id}", key);
        
        var validationResult = await updateVehiculoValidator.ValidateAsync(item);
        
        if (!validationResult.IsValid) {
            Log.Information("El vehículo contiene datos no válidos");
            var errorsDictionary = new Dictionary<string, string[]>(validationResult.ToDictionary());
            return BadRequestError.ValidationError.WithFieldErrors(errorsDictionary);
        }
        
        var existente = await repository.GetById(key);
        if (existente == null) {
            return Result.Failure<VehiculoDto, DomainError>(VehiculoError.NotFoundVehiculo(key));
        }
        
        if (item.Matricula != existente.Matrícula && await repository.ExistByMatricula(item.Matricula)) {
            return Result.Failure<VehiculoDto, DomainError>(VehiculoError.VehiculoYaExiste(item.Matricula));
        }
        
        var foto = await fotoRepository.GetById(item.Foto);
        if (foto == null) return FotoError.FotoNotFound(item.Foto);

        var mapeadoResult = item.ToVehiculoModel(foto);
        if (mapeadoResult.IsFailure) {
            return Result.Failure<VehiculoDto, DomainError>(mapeadoResult.Error);
        }

        var mapeado = mapeadoResult.Value;
        
        var vehiculo = existente with {
            Matrícula = mapeado.Matrícula,
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
            UpdatedAt = DateTime.UtcNow,
            Foto = mapeado.Foto,
            FotoFk = mapeado.FotoFk,
        };
        
        var result = await transactionsService.Update(vehiculo, existente.FotoFk);
        if (result.IsFailure) {
            return result.Error;
        }
        
        await cache.RemoveAsync(_vehiculoKey + key);
        await cache.RemoveAsync(_vehiculoKey + existente.Matrícula);
        await cache.RemoveAsync(_vehiculoKey + vehiculo.Matrícula);

        var dto = result.Value.ToDto();
        await cache.SetAsync(_vehiculoKey + key, dto);
        await cache.SetAsync(_vehiculoKey + vehiculo.Matrícula, dto);

        return Result.Success<VehiculoDto, DomainError>(dto);
    }

    /// <inheritdoc cref="IVehiculoService.SoftDelete" />
    public async Task<Result<bool, DomainError>> SoftDelete(string key) {
        Log.Debug("Borrado lógico de vehículo con ID {Id}", key);
        
        var existente = await repository.GetById(key);
        if (existente == null) {
            return Result.Failure<bool, DomainError>(VehiculoError.NotFoundVehiculo(key));
        }
        
        if (existente.Estado == EstadoVehiculo.EnAlquiler)
            return Result.Failure<bool, DomainError>(new BusinessRuleError("No se puede borrar un vehículo que está alquilado"));

        var result = await transactionsService.Delete(existente);
        if (result.IsFailure) {
            return result.Error;
        }
        
        await cache.RemoveAsync(_vehiculoKey + key);
        await cache.RemoveAsync(_vehiculoKey + existente.Matrícula);
        
        return Result.Success<bool, DomainError>(true);
    }
}