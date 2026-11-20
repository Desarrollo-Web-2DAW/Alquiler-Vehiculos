using Alquiler_Vehiculos.Cache;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Concesionario;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Mappers.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Repositories.Vehiculo.Concesionario;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Concesionario;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Alquiler_Vehiculos.Services.Vehiculo.Concesonario;

public class ConcesionarioService(
    IConcesionarioRepository repository,
    ICacheService cache,
    CreateConcesionarioValidator createConcesionarioValidator,
    UpdateConcesionarioValidator updateConcesionarioValidator) : IConcesionarioService
{
    private string _concesionarioKey => "concesionario:";

    /// <inheritdoc cref="IConcesionarioService.GetAll" />
    public async Task<IEnumerable<ConcesionarioDto>> GetAll() {
        Log.Debug("Consultando todas las categorías");
        var lista = await repository.GetAll();
        return lista.Select(c => c.ToDto());
    }

    /// <inheritdoc cref="IConcesionarioService.GetById" />
    public async Task<Result<ConcesionarioDto, DomainError>> GetById(long key) {
        Log.Debug("Consultando categoría con ID {Id}", key);
        var cached = await cache.GetAsync<ConcesionarioDto>(_concesionarioKey + key);
        if(cached != null) return Result.Success<ConcesionarioDto, DomainError>(cached);
        
        var concesionario = await repository.GetById(key);
        if (concesionario == null) return Result.Failure<ConcesionarioDto, DomainError>(ConcesionarioError.NotFoundConcesionario(key));
        await cache.SetAsync(_concesionarioKey + key, concesionario.ToDto());       
        return Result.Success<ConcesionarioDto, DomainError>(concesionario.ToDto());
    }
    
    /// <inheritdoc cref="IConcesionarioService.Create" />
    public async Task<Result<ConcesionarioDto, DomainError>> Create(CreateConcesionarioDto item) {
        Log.Debug("Creando categoría con nombre {Nombre}", item.Nombre);

        var validationResult = await createConcesionarioValidator.ValidateAsync(item);
        
        if (!validationResult.IsValid) {
            Log.Information("El concesionario contiene datos no validos");
            var errorsDictionary = new Dictionary<string, string[]>(validationResult.ToDictionary());
            return BadRequestError.ValidationError.WithFieldErrors(errorsDictionary);
        }
        if (await repository.ExistsByTelefono(item.Telefono)) return Result.Failure<ConcesionarioDto, DomainError>(ConcesionarioError.ConcesionarioYaExiste(item.Telefono));

        var concesionario = new ConcesionarioModel(0, item.Nombre, item.Ciudad, item.Telefono, item.Email, false, DateTime.UtcNow, null, null);
        var creado = await repository.Create(concesionario);
        return Result.Success<ConcesionarioDto, DomainError>(creado.ToDto());
    }

    /// <inheritdoc cref="DbLoggerCategory.Update" />
    public async Task<Result<ConcesionarioDto, DomainError>> Update(UpdateConcesionarioDto item, long key){
        Log.Debug("Actualizando categoría con ID {Id}", key);
        
        var validationResult = await updateConcesionarioValidator.ValidateAsync(item);
        
        if (!validationResult.IsValid) {
            Log.Information("El concesionario contiene datos no validos");
            var errorsDictionary = new Dictionary<string, string[]>(validationResult.ToDictionary());
            return BadRequestError.ValidationError.WithFieldErrors(errorsDictionary);
        }
        
        var existente = await repository.GetById(key);
        if (existente == null) return Result.Failure<ConcesionarioDto, DomainError>(ConcesionarioError.NotFoundConcesionario(key));
        
        if (item.Telefono != existente.Telefono && await repository.ExistsByTelefono(item.Telefono)) return Result.Failure<ConcesionarioDto, DomainError>(ConcesionarioError.ConcesionarioYaExiste(item.Telefono));
        
        var concesionario = new ConcesionarioModel(0, item.Nombre, item.Ciudad, item.Telefono, item.Email, false, DateTime.UtcNow, null, null);
        
        var actualizado = await repository.Update(key, concesionario);
        if (actualizado == null) return Result.Failure<ConcesionarioDto, DomainError>(ConcesionarioError.NotFoundConcesionario(key));

        await cache.RemoveAsync(_concesionarioKey + key);

        var vehiculos = await repository.GetVehiculos(key);
        foreach (var vehiculo in vehiculos) {
            await cache.RemoveAsync("vehiculo:" + vehiculo.Id);
            await cache.RemoveAsync("vehiculo:" + vehiculo.Matricula);
        }

        return Result.Success<ConcesionarioDto, DomainError>(actualizado.ToDto());
    }

    /// <inheritdoc cref="IConcesionarioService.SoftDelete" />
    public async Task<Result<bool, DomainError>> SoftDelete(long key) {
        Log.Debug("Borrado lógico de categoría con ID {Id}", key);
        
        if(await repository.HasVehiculos(key)) return Result.Failure<bool, DomainError>(new BusinessRuleError("No se puede borrar un concesionario con vehiculos asociados"));
        if(!await repository.ExistById(key)) return Result.Failure<bool, DomainError>(ConcesionarioError.NotFoundConcesionario(key));
        
        if(!await repository.SoftDelete(key)) return Result.Failure<bool, DomainError>(ConcesionarioError.BorradoError());
        
        await cache.RemoveAsync(_concesionarioKey + key);
        return Result.Success<bool, DomainError>(true);
    }
}