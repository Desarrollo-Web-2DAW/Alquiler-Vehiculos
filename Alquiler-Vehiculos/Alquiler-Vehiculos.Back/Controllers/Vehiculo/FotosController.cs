using Alquiler_Vehiculos.Errors.Storage;
using Alquiler_Vehiculos.Extensions;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Services.Storage;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Alquiler_Vehiculos.Controllers.Vehiculo;

/// <summary>
/// Gestiona las fotos del catálogo conservando sus ficheros históricos.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FotosController(IFotoService service, IStorageService storage) : ControllerBase {
    
    /// <summary>
    /// Consulta los metadatos de las fotografías activas sin identificarse.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Foto>>> GetAll() {
        return Ok(await service.GetAll());
    }

    /// <summary>
    /// Consulta una fotografía por su nombre generado, que actúa como identificador.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{nombre}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Foto>> GetById(string nombre) {
        return (await service.GetById(nombre)).Match(
            response => Ok(response),
            error => error.ToHttpResult<Foto>()
        );
    }

    /// <summary>
    /// Sirve el contenido de una imagen activa sin exigir identificación.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{nombre}/archivo")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetArchivo(string nombre) {
        var result = await service.GetById(nombre);
        if (result.IsFailure) return result.Error.ToHttpResult();
        return ServirArchivo(result.Value);
    }

    /// <summary>Consulta interna de metadatos, incluidas fotos retiradas del catálogo.</summary>
    [Authorize(Policy = PolicyNames.Trabajador)]
    [HttpGet("historico/{nombre}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Foto>> GetHistorico(string nombre) {
        return (await service.GetHistoricoById(nombre)).Match(
            response => Ok(response), error => error.ToHttpResult<Foto>());
    }

    /// <summary>
    /// Descarga interna de una foto histórica conservada en disco.
    /// </summary>
    [Authorize(Policy = PolicyNames.Trabajador)]
    [HttpGet("historico/{nombre}/archivo")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetArchivoHistorico(string nombre) {
        var result = await service.GetHistoricoById(nombre);
        if (result.IsFailure) return result.Error.ToHttpResult();
        return ServirArchivo(result.Value);
    }

    private ActionResult ServirArchivo(Foto foto) {
        var relativa = Path.Combine("vehiculos", foto.Nombre);
        if (!storage.FileExists(relativa)) return FotoError.FotoNotFound(foto.Nombre).ToHttpResult();
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(foto.Nombre, out var contentType)) contentType = "application/octet-stream";
        try {
            return File(System.IO.File.OpenRead(storage.GetFullPath(relativa)), contentType);
        } catch (FileNotFoundException) {
            return FotoError.FotoNotFound(foto.Nombre).ToHttpResult();
        } catch (DirectoryNotFoundException) {
            return FotoError.FotoNotFound(foto.Nombre).ToHttpResult();
        }
    }

    /// <summary>
    /// Sube una imagen nueva validando tamaño, extensión y MIME en almacenamiento.
    /// </summary>
    [Authorize(Policy = PolicyNames.Trabajador)]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(Foto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Foto>> Create([FromForm] IFormFile? archivo) {
        if (archivo is null) return BadRequest();
        return (await service.Create(archivo, "vehiculos")).Match(
            response => CreatedAtAction(nameof(GetById), new { nombre = response.Nombre }, response),
            error => error.ToHttpResult<Foto>()
        );
    }

    /// <summary>
    /// Da de baja la foto del catálogo sin eliminar el fichero ni sus relaciones.
    /// </summary>
    [Authorize(Policy = PolicyNames.Trabajador)]
    [HttpDelete("{nombre}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> Delete(string nombre) {
        return (await service.SoftDelete(nombre)).Match(
            response => Ok(response),
            error => error.ToHttpResult<bool>()
        );
    }
}