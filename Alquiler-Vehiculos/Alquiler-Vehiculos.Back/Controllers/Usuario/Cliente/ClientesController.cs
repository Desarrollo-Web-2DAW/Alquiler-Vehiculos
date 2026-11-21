using System.Security.Claims;
using Alquiler_Vehiculos.Dto.Clientes;
using Alquiler_Vehiculos.Dto.Common;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Extensions;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Services.Usuarios.Clientes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alquiler_Vehiculos.Controllers.Usuario.Cliente;
/// <summary>
/// Gestión de clientes (RF-13, RF-14, RF-46). El personal gestiona a cualquier cliente;
/// un cliente solo consulta y modifica sus propios datos; la baja es solo del administrador.
/// </summary>
[ApiController]
[Route("api/clientes")]
public class ClientesController(IClienteService servicio) : ControllerBase {
    /// <summary>Lista los clientes con búsqueda, filtro de estado y paginación.</summary>
    /// <param name="filtro">Texto, estado, inclusión de bajas, página y tamaño.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>200 con la página. Solo el administrador puede pedir las bajas.</returns>
    [Authorize(Policy = PolicyNames.Trabajador)]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ClienteResponseDto>>> GetAll(
        [FromQuery] ClienteFiltroDto filtro, CancellationToken ct) {
        var permitido = filtro with { IncluirBajas = filtro.IncluirBajas && EsAdmin() };
        var (items, totalPages, totalItems) = await servicio.GetPagedAsync(permitido, ct);

        return Ok(new PagedResponse<ClienteResponseDto> {
            Data = items.ToList(),
            Pagination = new PageMetadata(
                Math.Max(permitido.Page, 1), Math.Clamp(permitido.PageSize, 1, 100), totalPages, totalItems)
        });
    }

    /// <summary>Consulta un cliente por su identificador.</summary>
    /// <param name="id">Identificador del cliente.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>200 con el cliente, 403 si no es suyo, o 404 si no existe.</returns>
    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClienteResponseDto>> GetById(Guid id, CancellationToken ct) {
        if (!await PuedeAccederAsync(id, ct)) return Forbid();

        var result = await servicio.GetByIdAsync(id, EsAdmin(), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult<ClienteResponseDto>();
    }

    /// <summary>Da de alta un cliente de mostrador.</summary>
    /// <param name="dto">Datos del alta.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>201 con el cliente creado, 400 si los datos no son válidos o 409 si ya existe.</returns>
    [Authorize(Policy = PolicyNames.Trabajador)]
    [HttpPost]
    public async Task<ActionResult<ClienteResponseDto>> Create(
        [FromBody] ClienteCreateDto dto, CancellationToken ct) {
        var result = await servicio.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : result.Error.ToHttpResult<ClienteResponseDto>();
    }

    /// <summary>Modifica los datos de un cliente. El correo no se cambia.</summary>
    /// <param name="id">Identificador del cliente.</param>
    /// <param name="dto">Datos nuevos.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>200 con el cliente modificado, o 400, 403, 404 o 409.</returns>
    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClienteResponseDto>> Update(
        Guid id, [FromBody] ClienteUpdateDto dto, CancellationToken ct) {
        if (!await PuedeAccederAsync(id, ct)) return Forbid();

        var result = await servicio.UpdateAsync(id, dto, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult<ClienteResponseDto>();
    }

    /// <summary>Da de baja lógica a un cliente. Solo el administrador.</summary>
    /// <param name="id">Identificador del cliente.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>204 si se da de baja, o 404 si no existe.</returns>
    [Authorize(Policy = PolicyNames.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct) {
        var result = await servicio.DeleteAsync(id, ct);
        return result.IsSuccess
            ? NoContent()
            : result.Error.ToHttpResult();
    }

    private bool EsAdmin() {
        return User.IsInRole(nameof(RolUsuario.Admin));
    }

    private bool EsPersonal() {
        return User.IsInRole(nameof(RolUsuario.Trabajador))
               || User.IsInRole(nameof(RolUsuario.Responsable))
               || User.IsInRole(nameof(RolUsuario.Admin));
    }

    private async Task<bool> PuedeAccederAsync(Guid clienteId, CancellationToken ct) {
        if (EsPersonal()) return true;

        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(claim, out Guid usuarioId)) return false;

        var propio = await servicio.GetClienteIdDeUsuarioAsync(usuarioId, ct);
        return propio == clienteId;
    }
}