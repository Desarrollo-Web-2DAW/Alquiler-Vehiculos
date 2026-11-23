using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Query;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Extensions;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Services.Storage;
using Alquiler_Vehiculos.Services.Vehiculo;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alquiler_Vehiculos.Controllers.Vehiculo;

/// <summary>
/// Gestiona el catálogo de vehículos y sus bajas lógicas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class 
    VehiculoController(IVehiculoService service, IStorageService storageService) : ControllerBase {

    /// <summary>
    /// Consulta todos los vehículos para el personal de la oficina.
    /// </summary>
    [Authorize(Policy = PolicyNames.Trabajador)]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<VehiculoDto>>> GetAll() {
        return Ok(await service.GetAll());
    }

    /// <summary>
    /// Consulta los vehículos mediante paginación para el personal de la oficina.
    /// </summary>
    [Authorize(Policy = PolicyNames.Trabajador)]
    [HttpGet("paged")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<VehiculoDto>>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10) {
        if (page < 1 || pageSize < 1) return BadRequest("La página y el tamaño deben ser mayores que cero.");
        var (items, totalItems) = await service.GetPaged(page, pageSize);

        return Ok(new {
            Items = items,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
        });
    }

    /// <summary>
    /// Consulta un vehículo por su identificador.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(VehiculoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehiculoDto>> GetById(string id) {
        var result = await service.GetById(id);
        if (result.IsFailure) return result.Error.ToHttpResult<VehiculoDto>();
        if (!PuedeVerVehiculo(result.Value)) return VehiculoError.NotFoundVehiculo(id).ToHttpResult<VehiculoDto>();
        return Ok(result.Value);
    }

    /// <summary>
    /// Consulta una unidad concreta por su matrícula (RF-06).
    /// </summary>
    [AllowAnonymous]
    [HttpGet("matricula/{matricula}")]
    [ProducesResponseType(typeof(VehiculoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehiculoDto>> GetByMatricula(string matricula) {
        var result = await service.GetByMatricula(matricula);
        if (result.IsFailure) return result.Error.ToHttpResult<VehiculoDto>();
        if (!PuedeVerVehiculo(result.Value)) return VehiculoError.NotFoundVehiculoMatricula(matricula).ToHttpResult<VehiculoDto>();
        return Ok(result.Value);
    }

    /// <summary>
    /// Consulta el catálogo agrupado por marca, modelo y categoría (RF-06).
    /// </summary>
    /// <remarks>
    /// Solo el administrador ve también grupos sin unidades libres.
    /// La disponibilidad por fechas está pendiente de integración en el servicio.
    /// </remarks>
    [AllowAnonymous]
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<QueryResponseVehiculoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<QueryResponseVehiculoDto>>> GetByFilters([FromQuery] QueryRequestVehiculoDto request) {
        // El perfil procede del usuario autenticado, nunca de un parámetro de la petición.
        var esAdmin = User.IsInRole(nameof(RolUsuario.Admin));
        var result = await service.GetByFilters(request, esAdmin);
        return result.IsSuccess ? Ok(result.Value) : result.Error.ToHttpResult<IEnumerable<QueryResponseVehiculoDto>>();
    }

    /// <summary>
    /// Crea un vehículo recibiendo los datos campo por campo y archivo adjunto.
    /// </summary>
    [Authorize(Policy = PolicyNames.Admin)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(VehiculoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VehiculoDto>> Create(
        [FromForm] IFormFile foto,
        [FromForm] string matricula,
        [FromForm] string marca,
        [FromForm] string modelo,
        [FromForm] string acriss,
        [FromForm] string color,
        [FromForm] decimal precioBaseDia,
        [FromForm] int plazas,
        [FromForm] int maleteroLitros,
        [FromForm] int autonomiaKm,
        [FromForm] int kilometraje,
        [FromForm] string fechaUltimaRevision,
        [FromForm] long concesionario) 
    {
        var rutaFoto = await storageService.SaveFileAsync(foto, "vehiculos");

        if (rutaFoto.IsFailure) return rutaFoto.Error.ToHttpResult();
        
        var dto = new CreateVehiculoDto {
            Foto = rutaFoto.Value,
            Matricula = matricula,
            Marca = marca,
            Modelo = modelo,
            Acriss = acriss,
            Color = color,
            PrecioBaseDia = precioBaseDia,
            Plazas = plazas,
            MaleteroLitros = maleteroLitros,
            AutonomiaKm = autonomiaKm,
            Kilometraje = kilometraje,
            FechaUltimaRevision = fechaUltimaRevision,
            Concesionario = concesionario
        };
        
        var result = await EjecutarConFotoAsync(rutaFoto.Value, () => service.Create(dto));
        return result.Match(response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response), error => error.ToHttpResult<VehiculoDto>());
    }

    
    /// <summary>
    /// Actualiza un vehículo recibiendo los datos campo por campo y un archivo adjunto.
    /// </summary>
    [Authorize(Policy = PolicyNames.Trabajador)]
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(VehiculoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VehiculoDto>> Update(
        string id,
        [FromForm] IFormFile foto,
        [FromForm] string matricula,
        [FromForm] string marca,
        [FromForm] string modelo,
        [FromForm] string acriss,
        [FromForm] string color,
        [FromForm] decimal precioBaseDia,
        [FromForm] int plazas,
        [FromForm] int maleteroLitros,
        [FromForm] int autonomiaKm,
        [FromForm] int kilometraje,
        [FromForm] string fechaUltimaRevision,
        [FromForm] long concesionario)
    {
        var rutaFoto = await storageService.SaveFileAsync(foto, "vehiculos");

        if (rutaFoto.IsFailure) return rutaFoto.Error.ToHttpResult();

        var dto = new UpdateVehiculoDto {
            Foto = rutaFoto.Value,
            Matricula = matricula,
            Marca = marca,
            Modelo = modelo,
            Acriss = acriss,
            Color = color,
            PrecioBaseDia = precioBaseDia,
            Plazas = plazas,
            MaleteroLitros = maleteroLitros,
            AutonomiaKm = autonomiaKm,
            Kilometraje = kilometraje,
            FechaUltimaRevision = fechaUltimaRevision,
            Concesionario = concesionario
        };
        var result = await EjecutarConFotoAsync(rutaFoto.Value, () => service.Update(dto, id));
        return result.Match(response => Ok(response), error => error.ToHttpResult<VehiculoDto>());
    }


    /// <summary>
    /// Desactiva un vehículo mediante una baja lógica.
    /// </summary>
    [Authorize(Policy = PolicyNames.Admin)]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> Delete(string id) {
        return (await service.SoftDelete(id)).Match(response => Ok(response), error => error.ToHttpResult<bool>());
    }

    /// <summary>
    /// Restaura un vehículo previamente desactivado.
    /// </summary>
    [Authorize(Policy = PolicyNames.Admin)]
    [HttpPatch("{id}/restore")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehiculoDto>> Restore(string id) {
        return (await service.Restore(id)).Match(response => Ok(response), error => error.ToHttpResult<VehiculoDto>());
    }

    /// <summary>
    /// Si falla el vehículo se retira únicamente la subida nueva, nunca su foto anterior.
    /// </summary>
    private async Task<Result<VehiculoDto, DomainError>> EjecutarConFotoAsync(
        string nombre, Func<Task<Result<VehiculoDto, DomainError>>> operacion) {
        try {
            var result = await operacion();
            if (result.IsFailure) await storageService.DescartarFotoAsync(nombre);
            return result;
        } catch {
            await storageService.DescartarFotoAsync(nombre);
            throw;
        }
    }

    /// <summary>
    /// El personal identificado consulta la unidad y su histórico; el catálogo público
    /// y el cliente solo ven unidades activas y disponibles (RF-46).
    /// </summary>
    private bool PuedeVerVehiculo(VehiculoDto vehiculo) {
        var esPersonal = User?.Identity?.IsAuthenticated == true && (
            User.IsInRole(nameof(RolUsuario.Trabajador)) ||
            User.IsInRole(nameof(RolUsuario.Responsable)) ||
            User.IsInRole(nameof(RolUsuario.Admin)));

        return esPersonal || (vehiculo.IsActivo && vehiculo.Estado == nameof(EstadoVehiculo.Disponible));
    }
}