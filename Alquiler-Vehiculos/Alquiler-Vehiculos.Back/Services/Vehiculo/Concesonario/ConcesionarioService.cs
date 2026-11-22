using Alquiler_Vehiculos.Cache;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Concesionario;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Mappers.Vehiculo;
using Alquiler_Vehiculos.Repositories.Vehiculo.Concesionario;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Concesionario;
using CSharpFunctionalExtensions;
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
        Log.Debug("Consultando todos los concesionarios");
        var lista = await repository.GetAll();
        return lista.Select(c => c.ToDto());
    }

    /// <inheritdoc cref="IConcesionarioService.GetById" />
    public async Task<Result<ConcesionarioDto, DomainError>> GetById(long key) {
        Log.Debug("Consultando concesionario con ID {Id}", key);
        var cached = await cache.GetAsync<ConcesionarioDto>(_concesionarioKey + key);
        if(cached != null) return Result.Success<ConcesionarioDto, DomainError>(cached);
        
        var concesionario = await repository.GetById(key);
        if (concesionario == null) return Result.Failure<ConcesionarioDto, DomainError>(ConcesionarioError.NotFoundConcesionario(key));

        var dto = concesionario.ToDto();
        await cache.SetAsync(_concesionarioKey + key, dto);
        return Result.Success<ConcesionarioDto, DomainError>(dto);
    }
    
    /// <inheritdoc cref="IConcesionarioService.Create" />
    public async Task<Result<ConcesionarioDto, DomainError>> Create(CreateConcesionarioDto item) {
        Log.Debug("Creando concesionario con nombre {Nombre}", item.Nombre);

        var validationResult = await createConcesionarioValidator.ValidateAsync(item);
        
        if (!validationResult.IsValid) {
            Log.Information("El concesionario contiene datos no validos");
            var errorsDictionary = new Dictionary<string, string[]>(validationResult.ToDictionary());
            return ValidationError.WithFieldErrors(errorsDictionary);
        }
        if (await repository.ExistsByTelefono(item.Telefono)) return Result.Failure<ConcesionarioDto, DomainError>(ConcesionarioError.ConcesionarioYaExiste(item.Telefono));

        var concesionario = item.ToConcesionario();
        var creado = await repository.Create(concesionario);
        return Result.Success<ConcesionarioDto, DomainError>(creado.ToDto());
    }

    /// <inheritdoc cref="IConcesionarioService.Update" />
    public async Task<Result<ConcesionarioDto, DomainError>> Update(UpdateConcesionarioDto item, long key){
        Log.Debug("Actualizando concesionario con ID {Id}", key);
        
        var validationResult = await updateConcesionarioValidator.ValidateAsync(item);
        
        if (!validationResult.IsValid) {
            Log.Information("El concesionario contiene datos no validos");
            var errorsDictionary = new Dictionary<string, string[]>(validationResult.ToDictionary());
            return ValidationError.WithFieldErrors(errorsDictionary);
        }
        
        var existente = await repository.GetById(key);
        if (existente == null) return Result.Failure<ConcesionarioDto, DomainError>(ConcesionarioError.NotFoundConcesionario(key));
        
        if (item.Telefono != existente.Telefono && await repository.ExistsByTelefono(item.Telefono)) return Result.Failure<ConcesionarioDto, DomainError>(ConcesionarioError.ConcesionarioYaExiste(item.Telefono));
        
        var concesionario = existente with {
            Nombre = item.Nombre,
            Ciudad = item.Ciudad,
            Telefono = item.Telefono,
            Email = item.Email
        };
        
        var actualizado = await repository.Update(key, concesionario);
        if (actualizado == null) return Result.Failure<ConcesionarioDto, DomainError>(ConcesionarioError.NotFoundConcesionario(key));

        await cache.RemoveAsync(_concesionarioKey + key);

        var vehiculos = await repository.GetVehiculos(key);
        foreach (var vehiculo in vehiculos) {
            await cache.RemoveAsync("vehiculo:" + vehiculo.Id);
            await cache.RemoveAsync("vehiculo:" + vehiculo.Matrícula);
        }

        return Result.Success<ConcesionarioDto, DomainError>(actualizado.ToDto());
    }

    /// <inheritdoc cref="IConcesionarioService.SoftDelete" />
    public async Task<Result<bool, DomainError>> SoftDelete(long key) {
        Log.Debug("Borrado lógico de concesionario con ID {Id}", key);
        
        if(!await repository.ExistById(key)) return Result.Failure<bool, DomainError>(ConcesionarioError.NotFoundConcesionario(key));
        if(await repository.HasVehiculos(key)) return Result.Failure<bool, DomainError>(new BusinessRuleError("CONCESIONARIO_CON_VEHICULOS", "No se puede borrar un concesionario con vehiculos asociados"));
        
        if(!await repository.SoftDelete(key)) return Result.Failure<bool, DomainError>(ConcesionarioError.BorradoError());
        
        await cache.RemoveAsync(_concesionarioKey + key);
        return Result.Success<bool, DomainError>(true);
    }
}
