using Alquiler_Vehiculos.Dto.Oficina;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Extensions;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Services.Oficina;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alquiler_Vehiculos.Controllers.Oficina;

/// <summary>
/// Consulta y gestión de la oficina única de la empresa (RF-11, CU-18).
/// No expone POST ni DELETE: la oficina existe una sola vez desde la semilla.
/// </summary>
[ApiController]
[Route("api/oficina")]
[Produces("application/json")]
public class OficinaController(
    IOficinaService oficinaService,
    ILogger<OficinaController> logger
) : ControllerBase {
    /// <summary>Consulta los datos públicos, el horario semanal y el responsable de la oficina.</summary>
    /// <response code="200">Datos actuales de la oficina.</response>
    /// <response code="404">La oficina única no está inicializada.</response>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(OficinaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OficinaResponseDto>> Obtener(CancellationToken ct) {
        Result<OficinaResponseDto, DomainError> resultado = await oficinaService.ObtenerAsync(ct);

        return resultado.Match(
            oficina => Ok(oficina),
            error => error.ToHttpResult<OficinaResponseDto>());
    }

    /// <summary>Actualiza los datos de contacto y el horario semanal de la oficina.</summary>
    /// <response code="200">Oficina actualizada.</response>
    /// <response code="400">Formato o rango horario inválido.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="403">Requiere rol Responsable o Admin.</response>
    /// <response code="404">Oficina no inicializada.</response>
    [Authorize(Policy = PolicyNames.Responsable)]
    [HttpPut]
    [ProducesResponseType(typeof(OficinaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OficinaResponseDto>> Actualizar(
        [FromBody] UpdateOficinaDto dto,
        CancellationToken ct) {
        logger.LogInformation("Actualización de datos de oficina solicitada");

        Result<OficinaResponseDto, DomainError> resultado = await oficinaService.ActualizarAsync(dto, ct);

        return resultado.Match(
            oficina => Ok(oficina),
            error => error.ToHttpResult<OficinaResponseDto>());
    }

    /// <summary>Nombra un nuevo responsable de Operaciones para la oficina (CU-18).</summary>
    /// <response code="200">Responsable relevado y rol sincronizado.</response>
    /// <response code="400">Identificador vacío o inválido.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="403">Requiere rol Admin.</response>
    /// <response code="404">Oficina o trabajador no encontrado.</response>
    /// <response code="422">El trabajador está inactivo o no pertenece a Operaciones.</response>
    [Authorize(Policy = PolicyNames.Admin)]
    [HttpPatch("responsable")]
    [ProducesResponseType(typeof(OficinaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OficinaResponseDto>> CambiarResponsable(
        [FromBody] CambiarResponsableDto dto,
        CancellationToken ct) {
        logger.LogInformation("Cambio de responsable de oficina solicitado");

        Result<OficinaResponseDto, DomainError> resultado = await oficinaService.CambiarResponsableAsync(dto, ct);

        return resultado.Match(
            oficina => Ok(oficina),
            error => error.ToHttpResult<OficinaResponseDto>());
    }
}
