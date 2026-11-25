using Alquiler_Vehiculos.Cache;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Query;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Storage;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Mappers.Vehiculo;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;
using Alquiler_Vehiculos.Repositories.Vehiculo.Concesionario;
using Alquiler_Vehiculos.Repositories.Vehiculo.Marca;
using Alquiler_Vehiculos.Services.Reserva;
using Alquiler_Vehiculos.Services.Storage;
using Alquiler_Vehiculos.Services.Taller;
using Alquiler_Vehiculos.Services.Vehiculo.Transactions;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Query;
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
    IFotoService fotoService,
    IVehiculoTransactionsService transactionsService,
    ICacheService cache,
    CreateVehiculoValidator createVehiculoValidator,
    UpdateVehiculoValidator updateVehiculoValidator,
    IConcesionarioRepository concesionarioRepository,
    QueryRequestVehiculoValidator queryRequestVehiculoValidator,
    IMarcaRepository marcaRepository,
    IParteTallerService tallerService
    ) : IVehiculoService {
    
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
    
    
    /// <inheritdoc cref="IVehiculoService.GetByMatricula" />
    public async Task<Result<VehiculoDto, DomainError>> GetByMatricula(string matricula) {
        Log.Debug("Consultando vehículo con matricula {Matricula}", matricula);

        var cached = await cache.GetAsync<VehiculoDto>(ClaveMatricula + matricula);
        if (cached is not null) return Result.Success<VehiculoDto, DomainError>(cached);

        var vehiculo = await repository.GetByMatricula(matricula);
        if (vehiculo is null) return Result.Failure<VehiculoDto, DomainError>(VehiculoError.NotFoundVehiculoMatricula(matricula));

        var dto = vehiculo.ToDto();
        await cache.SetAsync(ClaveVehiculo + dto.Id, dto);
        await cache.SetAsync(ClaveMatricula + vehiculo.Matrícula, dto);

        return Result.Success<VehiculoDto, DomainError>(dto);
    }

    /// <inheritdoc cref="IVehiculoService.GetByFilters" />
    public async Task<Result<IEnumerable<QueryResponseVehiculoDto>, DomainError>> GetByFilters(QueryRequestVehiculoDto request, bool esAdmin = false) {
        Log.Debug("Consultando el catálogo de vehículos por filtros");

        var validationResult = await queryRequestVehiculoValidator.ValidateAsync(request);
        if (!validationResult.IsValid) {
            var errorsDictionary = new Dictionary<string, string[]>(validationResult.ToDictionary());
            return ValidationError.WithFieldErrors(errorsDictionary);
        }

        if (request.FechaRecogida.HasValue && request.FechaDevolucion.HasValue) {
            // TODO: integrar aquí la consulta real de disponibilidad y la comprobación
            // del horario de oficina cuando el equipo defina el contrato correspondiente.
            // Es una dependencia pendiente, no un error en un rango válido del cliente.
            return new InternalError("La disponibilidad por fechas aún no está integrada con el módulo de reservas.");
        }

        var vehiculos = await repository.GetByFilters(request);

        // TODO: cuando exista el contrato de disponibilidad por fechas, consultar ese módulo
        // y contar como libres las unidades cuyos IDs aparezcan en su resultado.
        // No se deben quitar los ocupados antes de agrupar: también forman parte del total.
        var grupos = vehiculos
            .GroupBy(v => new { v.Marca, v.Modelo, v.Carroceria })
            .Select(grupo => new QueryResponseVehiculoDto {
                Marca = grupo.Key.Marca,
                Modelo = grupo.Key.Modelo,
                Categoria = grupo.Key.Carroceria.ToString(),
                UnidadesTotales = grupo.Count(),
                UnidadesLibres = grupo.Count(v => v.Activo && v.Estado == EstadoVehiculo.Disponible)
            });

        // Solo el administrador conserva también los grupos sin unidades libres.
        if (!esAdmin) grupos = grupos.Where(grupo => grupo.UnidadesLibres > 0);
        return Result.Success<IEnumerable<QueryResponseVehiculoDto>, DomainError>(grupos.ToList());
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

        var catalogo = await ValidarMarcaModeloAsync(item.Marca, item.Modelo);
        if (catalogo.IsFailure) return catalogo.Error;

        var fotoCreada = await fotoService.Create(item.Foto!, "vehiculos");
        if (fotoCreada.IsFailure) return Result.Failure<VehiculoDto, DomainError>(fotoCreada.Error);

        var vehiculo = item.ToVehiculo(fotoCreada.Value);
        if (vehiculo.IsFailure) {
            await DescartarFotoNuevaAsync(fotoCreada.Value.Nombre);
            return Result.Failure<VehiculoDto, DomainError>(vehiculo.Error);
        }

        Result<VehiculoModel, DomainError> creado;
        try {
            creado = await repository.Create(vehiculo.Value);
        } catch {
            await DescartarFotoNuevaAsync(fotoCreada.Value.Nombre);
            throw;
        }
        if (creado.IsFailure) {
            await DescartarFotoNuevaAsync(fotoCreada.Value.Nombre);
            return Result.Failure<VehiculoDto, DomainError>(creado.Error);
        }
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

        if (await concesionarioRepository.GetById(item.Concesionario) is null)
            return ValidationError.Create($"El concesionario con ID '{item.Concesionario}' no existe o está dado de baja.");

        var catalogo = await ValidarMarcaModeloAsync(item.Marca, item.Modelo);
        if (catalogo.IsFailure) return catalogo.Error;

        var fotoPreparada = await fotoService.PrepareAsync(item.Foto!, "vehiculos");
        if (fotoPreparada.IsFailure) return Result.Failure<VehiculoDto, DomainError>(fotoPreparada.Error);

        var mapeadoResult = item.ToVehiculoModel(fotoPreparada.Value);
        if (mapeadoResult.IsFailure)
        {
            Log.Warning("El archivo preparado {Nombre} queda conservado porque los datos del vehículo no se pudieron mapear: {Error}",
                fotoPreparada.Value.Nombre, mapeadoResult.Error);
            return Result.Failure<VehiculoDto, DomainError>(mapeadoResult.Error);
        }

        var vehiculo = mapeadoResult.Value;
        Result<UpdateVehiculoResult, DomainError> result;
        try {
            result = await transactionsService.UpdateAsync(key, vehiculo);
        } catch (Exception ex) {
            Log.Error(ex, "Falló la actualización del vehículo {Id}; se conserva el archivo preparado {Nombre}",
                key, fotoPreparada.Value.Nombre);
            throw;
        }
        if (result.IsFailure)
        {
            Log.Warning("No se actualizó el vehículo {Id}; se conserva el archivo preparado {Nombre}: {Error}",
                key, fotoPreparada.Value.Nombre, result.Error);
            return Result.Failure<VehiculoDto, DomainError>(result.Error);
        }

        var actualizado = result.Value.Vehiculo;
        await cache.RemoveAsync(ClaveVehiculo + key);
        await cache.RemoveAsync(ClaveMatricula + result.Value.MatriculaAnterior);
        if (actualizado.Matrícula != result.Value.MatriculaAnterior)
            await cache.RemoveAsync(ClaveMatricula + actualizado.Matrícula);

        var dto = actualizado.ToDto();
        await cache.SetAsync(ClaveVehiculo + key, dto);
        await cache.SetAsync(ClaveMatricula + actualizado.Matrícula, dto);

        return Result.Success<VehiculoDto, DomainError>(dto);
    }

    /// <inheritdoc cref="IVehiculoService.SoftDelete" />
    public async Task<Result<bool, DomainError>> SoftDelete(string key)
    {
        Log.Debug("Borrado lógico de vehículo con ID {Id}", key);
        
        var vehiculo = await repository.GetById(key);
        if (vehiculo == null) return VehiculoError.NotFoundVehiculo(key);
        
        if (vehiculo.Estado != EstadoVehiculo.Disponible) return VehiculoError.NoDisponibleParaBaja(vehiculo.Estado);
        
        var partesAbiertos = await tallerService.TienePartesAbiertosAsync(key);
        if (partesAbiertos.IsFailure) return partesAbiertos.Error;
        if (partesAbiertos.Value)  return VehiculoError.VehiculoConPartesAbiertos(key);

        // var tieneReserva = await repository.TieneReservas(key);
        // if (tieneReserva) return VehiculoError.NoDisponibleParaBaja();

        var result = await repository.SoftDelete(key);

        await cache.RemoveAsync(ClaveVehiculo + key);
        await cache.RemoveAsync(ClaveMatricula + vehiculo.Matrícula);

        return Result.Success<bool, DomainError>(result);
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

    /// <summary>
    /// Comprueba la combinación contra las marcas activas y sus modelos del catálogo.
    /// Los nombres deben coincidir con los almacenados; no se alteran datos históricos.
    /// </summary>
    private async Task<Result<bool, DomainError>> ValidarMarcaModeloAsync(string nombreMarca, string nombreModelo) {
        var marcas = await marcaRepository.GetAll();
        var marca = marcas.FirstOrDefault(m => !m.IsDeleted && m.Nombre == nombreMarca);
        if (marca is null) {
            return ValidationError.WithFieldErrors(new Dictionary<string, string[]> {
                [nameof(CreateVehiculoDto.Marca)] = ["La marca indicada no existe o está dada de baja."]
            });
        }

        if (!marca.ModelosDisponibles.Contains(nombreModelo)) {
            return ValidationError.WithFieldErrors(new Dictionary<string, string[]> {
                [nameof(CreateVehiculoDto.Modelo)] = [$"El modelo indicado no pertenece a la marca '{marca.Nombre}'."]
            });
        }

        return Result.Success<bool, DomainError>(true);
    }

    private async Task DescartarFotoNuevaAsync(string nombre) {
        try {
            var resultado = await fotoService.SoftDelete(nombre);
            if (resultado.IsFailure) {
                Log.Error("No se pudo descartar la foto nueva {Nombre}: {Error}", nombre, resultado.Error);
            }
        } catch (Exception ex) {
            Log.Error(ex, "No se pudo descartar la foto nueva {Nombre}", nombre);
        }
    }
}
