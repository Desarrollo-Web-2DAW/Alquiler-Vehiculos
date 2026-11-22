using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Concesionario;
using Alquiler_Vehiculos.Extensions;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Services.Vehiculo.Concesonario;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alquiler_Vehiculos.Controllers.Vehiculo;

/// <summary>
/// Gestiona el catálogo de concesionarios y sus bajas lógicas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PolicyNames.Admin)]
public class ConcesionarioController(IConcesionarioService service) : ControllerBase {
    
    /// <summary>
    /// Consulta los concesionarios activos.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConcesionarioDto>>> GetAll() {
        return Ok(await service.GetAll());
    }

    /// <summary>
    /// Consulta un concesionario por su identificador numérico.
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConcesionarioDto>> GetById(long id) {
        return (await service.GetById(id)).Match(
            response => Ok(response),
            error => error.ToHttpResult<ConcesionarioDto>()
        );
    }

    /// <summary>
    /// Crea un concesionario y devuelve la ubicación del recurso.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ConcesionarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ConcesionarioDto>> Create([FromBody] CreateConcesionarioDto dto) {
        return (await service.Create(dto)).Match(
            response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response),
            error => error.ToHttpResult<ConcesionarioDto>()
        );
    }

    /// <summary>
    /// Sustituye los datos de un concesionario.
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ConcesionarioDto>> Update(long id, [FromBody] UpdateConcesionarioDto dto) {
        return (await service.Update(dto, id)).Match(
            response => Ok(response),
            error => error.ToHttpResult<ConcesionarioDto>()
        );
    }

    /// <summary>
    /// Desactiva el concesionario conservando los vehículos adquiridos en él.
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> Delete(long id) {
        return (await service.SoftDelete(id)).Match(
            response => Ok(response),
            error => error.ToHttpResult<bool>()
        );
    }
}