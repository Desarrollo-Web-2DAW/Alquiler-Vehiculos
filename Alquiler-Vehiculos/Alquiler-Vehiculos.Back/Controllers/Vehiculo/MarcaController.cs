using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Marca;
using Alquiler_Vehiculos.Extensions;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Services.Vehiculo.Marca;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alquiler_Vehiculos.Controllers.Vehiculo;

/// <summary>
/// Gestiona el catálogo de marcas y sus bajas lógicas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PolicyNames.Admin)]
public class MarcaController(IMarcaService service) : ControllerBase {
    
    /// <summary>
    /// Consulta las marcas activas del catálogo.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MarcaDto>>> GetAll() {
        return Ok(await service.GetAll());
    }

    /// <summary>
    /// Consulta una marca por su ObjectId.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MarcaDto>> GetById(string id) {
        return (await service.GetById(id)).Match(
            response => Ok(response),
            error => error.ToHttpResult<MarcaDto>()
        );
    }

    /// <summary>
    /// Crea una marca con sus modelos disponibles.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MarcaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MarcaDto>> Create([FromBody] CreateMarcaDto dto) {
        return (await service.Create(dto)).Match(
            response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response),
            error => error.ToHttpResult<MarcaDto>()
        );
    }

    /// <summary>
    /// Sustituye los datos y la lista completa de modelos de una marca.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MarcaDto>> Update(string id, [FromBody] UpdateMarcaDto dto) {
        return (await service.Update(dto, id)).Match(
            response => Ok(response),
            error => error.ToHttpResult<MarcaDto>()
        );
    }

    /// <summary>
    /// Desactiva la marca sin borrar sus vehículos ni su histórico.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> Delete(string id) {
        return (await service.SoftDelete(id)).Match(
            response => Ok(response),
            error => error.ToHttpResult<bool>()
        );
    }
}