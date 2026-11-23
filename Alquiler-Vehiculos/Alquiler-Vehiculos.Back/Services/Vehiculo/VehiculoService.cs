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
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;
using Alquiler_Vehiculos.Repositories.Vehiculo.Concesionario;
using Alquiler_Vehiculos.Repositories.Vehiculo.Marca;
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
    IFotoRepository fotoRepository,
    IVehiculoTransactionsService transactionsService,
    ICacheService cache,
    CreateVehiculoValidator createVehiculoValidator,
    UpdateVehiculoValidator updateVehiculoValidator,
    IConcesionarioRepository concesionarioRepository,
    QueryRequestVehiculoValidator queryRequestVehiculoValidator,
    IMarcaRepository marcaRepository
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

        var foto = await ObtenerFotoActivaAsync(item.Foto);
        if (foto.IsFailure) return Result.Failure<VehiculoDto, DomainError>(foto.Error);

        var vehiculo = item.ToVehiculo(foto.Value);
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

        var catalogo = await ValidarMarcaModeloAsync(item.Marca, item.Modelo);
        if (catalogo.IsFailure) return catalogo.Error;

        var foto = await ObtenerFotoActivaAsync(item.Foto);
        if (foto.IsFailure) return Result.Failure<VehiculoDto, DomainError>(foto.Error);

        var mapeadoResult = item.ToVehiculoModel(foto.Value);
        if (mapeadoResult.IsFailure)
        {
            return Result.Failure<VehiculoDto, DomainError>(mapeadoResult.Error);
        }

        var mapeado = mapeadoResult.Value;

        // Se conserva Id, auditoría, Estado y Activo: el PUT no realiza transiciones ni bajas.
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

    /// <summary>
    /// Recupera la foto indicada exigiendo que sea la <b>activa</b>.
    /// </summary>
    /// <remarks>
    /// Un vehículo solo puede apuntar a una foto viva: <see cref="Foto.Activo"/> es la
    /// fuente de verdad, no las referencias. Una foto dada de baja se trata como inexistente,
    /// de modo que el vehículo nunca queda con una imagen retirada.
    /// </remarks>
    /// <param name="nombre">Clave de la foto.</param>
    /// <returns>La foto activa, o <see cref="FotoError.FotoNotFound"/> si no existe o está de baja.</returns>
    private async Task<Result<Foto, DomainError>> ObtenerFotoActivaAsync(string nombre)
    {
        var foto = await fotoRepository.GetById(nombre);

        if (foto is null || !foto.Activo || foto.DeletedAt is not null)
        {
            return Result.Failure<Foto, DomainError>(FotoError.FotoNotFound(nombre));
        }

        return Result.Success<Foto, DomainError>(foto);
    }
}
