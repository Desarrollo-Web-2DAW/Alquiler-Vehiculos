using Alquiler_Vehiculos.Dto.Taller;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Usuarios;
using Alquiler_Vehiculos.Extensions;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using Alquiler_Vehiculos.Repositories.Trabajadores;
using Alquiler_Vehiculos.Services.Taller;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alquiler_Vehiculos.Controllers.Taller;

/// <summary>
/// Partes de taller (RF-08): apertura, cierre y consultas del documento interno del
/// taller. Los clientes no tienen acceso (RF-46): abrir y cerrar es solo para el
/// tipo Mecánico (RF-12) y las consultas son para el personal interno.
/// </summary>
[ApiController]
[Route("api/taller")]
[Produces("application/json")]
public class ParteTallerController(
    IParteTallerService parteTallerService,
    ITrabajadorRepository trabajadores,
    ILogger<ParteTallerController> logger
) : ControllerBase {
    /// <summary>
    /// Abre un parte para la entrada de un vehículo al taller (RF-08, CU-07).
    /// </summary>
    /// <response code="201">Parte abierto.</response>
    /// <response code="400">Datos de apertura inválidos.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="403">Requiere trabajador de tipo Mecánico (RF-12).</response>
    /// <response code="409">El vehículo ya tiene un parte abierto (RF-08).</response>
    /// <response code="422">El trabajador está inactivo.</response>
    [Authorize(Policy = PolicyNames.Mecanico)]
    [HttpPost("partes")]
    [ProducesResponseType(typeof(ParteTallerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ParteTallerDto>> Abrir(
        [FromBody] AbrirParteDto dto,
        CancellationToken ct) {
        var tramitador = await ObtenerTramitadorAsync();
        if (tramitador.IsFailure) {
            return tramitador.Error.ToHttpResult<ParteTallerDto>();
        }

        logger.LogInformation(
            "Apertura de parte de taller para el vehículo {VehiculoId}", dto.VehiculoId);

        Result<ParteTallerDto, DomainError> resultado =
            await parteTallerService.AbrirParteAsync(tramitador.Value, dto, ct);

        return resultado.Match(
            parte => CreatedAtAction(nameof(ObtenerPorId), new { id = parte.Id }, parte),
            error => error.ToHttpResult<ParteTallerDto>());
    }

    /// <summary>
    /// Cierra un parte al terminar la reparación (RF-08, CU-07).
    /// </summary>
    /// <response code="200">Parte cerrado.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="403">Requiere trabajador de tipo Mecánico (RF-12).</response>
    /// <response code="404">El parte no existe.</response>
    /// <response code="409">El parte ya estaba cerrado (RF-32).</response>
    /// <response code="422">El trabajador está inactivo.</response>
    [Authorize(Policy = PolicyNames.Mecanico)]
    [HttpPut("partes/{id:guid}/cierre")]
    [ProducesResponseType(typeof(ParteTallerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ParteTallerDto>> Cerrar(
        Guid id,
        CancellationToken ct) {
        var tramitador = await ObtenerTramitadorAsync();
        if (tramitador.IsFailure) {
            return tramitador.Error.ToHttpResult<ParteTallerDto>();
        }

        logger.LogInformation("Cierre del parte de taller {ParteId}", id);

        Result<ParteTallerDto, DomainError> resultado =
            await parteTallerService.CerrarParteAsync(id, tramitador.Value, ct);

        return resultado.Match(
            parte => Ok(parte),
            error => error.ToHttpResult<ParteTallerDto>());
    }

    /// <summary>Consulta un parte por su identificador.</summary>
    /// <response code="200">Datos del parte.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="403">Los clientes no ven partes (RF-46).</response>
    /// <response code="404">El parte no existe.</response>
    [Authorize(Policy = PolicyNames.Trabajador)]
    [HttpGet("partes/{id:guid}")]
    [ProducesResponseType(typeof(ParteTallerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParteTallerDto>> ObtenerPorId(
        Guid id,
        CancellationToken ct) {
        Result<ParteTallerDto, DomainError> resultado =
            await parteTallerService.ObtenerPorIdAsync(id, ct);

        return resultado.Match(
            parte => Ok(parte),
            error => error.ToHttpResult<ParteTallerDto>());
    }

    /// <summary>
    /// Vehículos que están en el taller ahora mismo con su fecha de entrada (CU-08).
    /// </summary>
    /// <response code="200">Partes abiertos ordenados por fecha de entrada.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="403">Los clientes no ven partes (RF-46).</response>
    [Authorize(Policy = PolicyNames.Trabajador)]
    [HttpGet("partes/abiertos")]
    [ProducesResponseType(typeof(IReadOnlyList<ParteTallerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ParteTallerDto>>> ListarAbiertos(
        CancellationToken ct) {
        Result<IReadOnlyList<ParteTallerDto>, DomainError> resultado =
            await parteTallerService.ListarAbiertosAsync(ct);

        return resultado.Match(
            abiertos => Ok(abiertos),
            error => error.ToHttpResult<IReadOnlyList<ParteTallerDto>>());
    }

    /// <summary>Historial completo de partes de un vehículo.</summary>
    /// <response code="200">Partes del vehículo por fecha de entrada.</response>
    /// <response code="400">Identificador de vehículo ausente.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="403">Los clientes no ven partes (RF-46).</response>
    [Authorize(Policy = PolicyNames.Trabajador)]
    [HttpGet("vehiculos/{vehiculoId}/partes")]
    [ProducesResponseType(typeof(IReadOnlyList<ParteTallerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ParteTallerDto>>> ListarPorVehiculo(
        string vehiculoId,
        CancellationToken ct) {
        Result<IReadOnlyList<ParteTallerDto>, DomainError> resultado =
            await parteTallerService.ListarPorVehiculoAsync(vehiculoId, ct);

        return resultado.Match(
            historial => Ok(historial),
            error => error.ToHttpResult<IReadOnlyList<ParteTallerDto>>());
    }

    /// <summary>
    /// Indica si un vehículo tiene un parte abierto: es la comprobación que pide
    /// RF-01 antes de dar de baja un vehículo («sin partes abiertos», si no, 409).
    /// </summary>
    /// <response code="200">Resultado de la comprobación.</response>
    /// <response code="400">Identificador de vehículo ausente.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="403">Los clientes no ven partes (RF-46).</response>
    [Authorize(Policy = PolicyNames.Trabajador)]
    [HttpGet("vehiculos/{vehiculoId}/partes/abiertos")]
    [ProducesResponseType(typeof(TienePartesAbiertosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TienePartesAbiertosDto>> TienePartesAbiertos(
        string vehiculoId,
        CancellationToken ct) {
        Result<bool, DomainError> resultado =
            await parteTallerService.TienePartesAbiertosAsync(vehiculoId, ct);

        return resultado.Match(
            tiene => Ok(new TienePartesAbiertosDto(tiene)),
            error => error.ToHttpResult<TienePartesAbiertosDto>());
    }

    /// <summary>
    /// Resuelve quién tramita a partir del token (claim <c>trabajador_id</c>).
    /// </summary>
    private async Task<Result<Trabajador, DomainError>> ObtenerTramitadorAsync() {
        var claim = User.FindFirst("trabajador_id")?.Value;
        if (!Guid.TryParse(claim, out var trabajadorId)) {
            return Result.Failure<Trabajador, DomainError>(
                new ForbiddenError("SIN_TRABAJADOR", "La cuenta autenticada no tiene un trabajador asociado"));
        }

        var trabajador = await trabajadores.GetById(trabajadorId);
        if (trabajador is null) {
            return Result.Failure<Trabajador, DomainError>(TrabajadorError.NotFound(trabajadorId));
        }

        return Result.Success<Trabajador, DomainError>(trabajador);
    }
}
