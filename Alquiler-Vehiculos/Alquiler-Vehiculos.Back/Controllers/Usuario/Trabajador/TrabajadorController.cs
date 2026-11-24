using Alquiler_Vehiculos.Dto.Trabajadores;
using Alquiler_Vehiculos.Extensions;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Services.Usuarios.Trabajadores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alquiler_Vehiculos.Controllers.Usuario.Trabajador;

/// <summary>
/// Gestión de trabajadores de la oficina (RF-12, RF-46, CU-18).
/// Solo accesible por el perfil Administrador.
/// </summary>
[ApiController]
[Route("api/trabajadores")]
[Authorize(Policy = PolicyNames.Admin)]
public class TrabajadorController(ITrabajadorService trabajadorService) : ControllerBase {
    /// <summary>Devuelve el listado de trabajadores no dados de baja.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TrabajadorResponseDto>>> GetAll() {
        var trabajadores = await trabajadorService.GetAllAsync();
        return Ok(trabajadores);
    }
    /// <summary>Devuelve un trabajador por su identificador.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TrabajadorResponseDto>> GetById(Guid id) {
        var result = await trabajadorService.GetByIdAsync(id);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult<TrabajadorResponseDto>();
    }
    /// <summary>Da de alta un trabajador junto con su cuenta de usuario y rol Trabajador.</summary>
    [HttpPost]
    public async Task<ActionResult<TrabajadorResponseDto>> Create([FromBody] CreateTrabajadorDto dto) {
        var result = await trabajadorService.CreateAsync(dto);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : result.Error.ToHttpResult<TrabajadorResponseDto>();
    }
    /// <summary>Modifica los datos de un trabajador existente.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TrabajadorResponseDto>> Update(Guid id, [FromBody] UpdateTrabajadorDto dto) {
        var result = await trabajadorService.UpdateAsync(id, dto);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult<TrabajadorResponseDto>();
    }
    /// <summary>Da de baja lógica a un trabajador (RF-12, CU-18).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id) {
        var result = await trabajadorService.DeleteAsync(id);
        return result.IsSuccess
            ? NoContent()
            : result.Error.ToHttpResult();
    }
    /// <summary>Reactiva un trabajador dado de baja lógica.</summary>
    [HttpPatch("{id:guid}/restore")]
    public async Task<ActionResult<TrabajadorResponseDto>> Restore(Guid id) {
        var result = await trabajadorService.RestoreAsync(id);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult<TrabajadorResponseDto>();
    }
}