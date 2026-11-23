using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Extensions;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Services.Usuarios.Users;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alquiler_Vehiculos.Controllers.Usuario.User;

/// <summary>
/// Administración de cuentas de usuario reservada exclusivamente al rol Admin (RF-46).
/// </summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize(Policy = PolicyNames.Admin)]
public class UsersController(
    IUserService userService,
    ILogger<UsersController> logger
) : ControllerBase {
    /// <summary>Lista todas las cuentas registradas en el sistema.</summary>
    /// <response code="200">Listado completo de usuarios con su rol y estado.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="403">El usuario autenticado no tiene rol Admin.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDetalleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<UserDetalleDto>>> GetAll(CancellationToken ct) {
        logger.LogInformation("Listado administrativo de usuarios solicitado");

        Result<IReadOnlyList<UserDetalleDto>, DomainError> resultado = await userService.GetAllAsync(ct);

        return resultado.Match(
            usuarios => Ok(usuarios),
            error => error.ToHttpResult<IReadOnlyList<UserDetalleDto>>()
        );
    }

    /// <summary>Obtiene el detalle administrativo de una cuenta por su identificador.</summary>
    /// <param name="id">Identificador único de la cuenta.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="200">Detalle de la cuenta encontrada.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="403">El usuario autenticado no tiene rol Admin.</response>
    /// <response code="404">No existe ninguna cuenta con ese identificador.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetalleDto>> GetById(Guid id, CancellationToken ct) {
        logger.LogInformation("Consulta administrativa de cuenta {UserId}", id);

        Result<UserDetalleDto, DomainError> resultado = await userService.GetByIdAsync(id, ct);

        return resultado.Match(
            usuario => Ok(usuario),
            error => error.ToHttpResult<UserDetalleDto>()
        );
    }

    /// <summary>Cambia el rol de una cuenta respetando las reglas de compatibilidad y el último Admin.</summary>
    /// <param name="id">Identificador de la cuenta.</param>
    /// <param name="dto">Nuevo rol solicitado.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="200">Cuenta actualizada con su nuevo rol.</response>
    /// <response code="400">Rol inválido o vacío.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="403">El usuario autenticado no tiene rol Admin.</response>
    /// <response code="404">Cuenta no encontrada.</response>
    /// <response code="409">Rol incompatible con la persona o intento de quitar el último Admin.</response>
    [HttpPatch("{id:guid}/role")]
    [ProducesResponseType(typeof(UserDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDetalleDto>> CambiarRol(
        Guid id,
        [FromBody] CambiarRolDto dto,
        CancellationToken ct) {
        logger.LogInformation("Solicitud de cambio de rol para cuenta {UserId}", id);

        Result<UserDetalleDto, DomainError> resultado = await userService.CambiarRolAsync(id, dto, ct);

        return resultado.Match(
            usuario => Ok(usuario),
            error => error.ToHttpResult<UserDetalleDto>()
        );
    }

    /// <summary>Da de baja lógica una cuenta sin eliminar sus datos históricos.</summary>
    /// <param name="id">Identificador de la cuenta.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="204">Cuenta desactivada correctamente.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="403">El usuario autenticado no tiene rol Admin.</response>
    /// <response code="404">Cuenta no encontrada.</response>
    /// <response code="409">No se puede desactivar el último Admin activo.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Desactivar(Guid id, CancellationToken ct) {
        logger.LogInformation("Solicitud de baja lógica para cuenta {UserId}", id);

        UnitResult<DomainError> resultado = await userService.DesactivarAsync(id, ct);

        return resultado.IsSuccess
            ? NoContent()
            : resultado.Error.ToHttpResult();
    }

    /// <summary>Reactiva una cuenta previamente dada de baja lógica.</summary>
    /// <param name="id">Identificador de la cuenta.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="200">Cuenta reactivada correctamente.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="403">El usuario autenticado no tiene rol Admin.</response>
    /// <response code="404">Cuenta no encontrada.</response>
    [HttpPatch("{id:guid}/reactivate")]
    [ProducesResponseType(typeof(UserDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetalleDto>> Reactivar(Guid id, CancellationToken ct) {
        logger.LogInformation("Solicitud de reactivación para cuenta {UserId}", id);

        Result<UserDetalleDto, DomainError> resultado = await userService.ReactivarAsync(id, ct);

        return resultado.Match(
            usuario => Ok(usuario),
            error => error.ToHttpResult<UserDetalleDto>()
        );
    }
}
