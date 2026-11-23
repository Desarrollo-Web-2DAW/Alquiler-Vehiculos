using System.Security.Claims;
using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Errors.Auth;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Extensions;
using Alquiler_Vehiculos.Services.Usuarios.Auth;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alquiler_Vehiculos.Controllers.Usuario.User;

/// <summary>
/// Controlador de autenticación: inicio de sesión, alta de clientes y perfil actual (RF-13, RF-46).
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(
    IAuthService authService,
    ILogger<AuthController> logger
) : ControllerBase {
    /// <summary>Inicia sesión y devuelve el token JWT.</summary>
    /// <param name="loginDto">Email y contraseña.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>200 con el token, o el error de dominio mapeado a HTTP.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto loginDto,
        CancellationToken ct) {
        logger.LogInformation("Petición de inicio de sesión recibida");

        Result<AuthResponseDto, DomainError> resultado = await authService.LoginAsync(loginDto, ct);

        return resultado.Match(
            response => Ok(response),
            error => error.ToHttpResult<AuthResponseDto>()
        );
    }

    /// <summary>
    /// Registra un nuevo cliente con rol Cliente e inicia sesión devolviendo un JWT.
    /// </summary>
    /// <remarks>La contraseña debe tener al menos 8 caracteres e incluir mayúscula, minúscula, número y símbolo.</remarks>
    /// <response code="201">Cliente y cuenta creados; devuelve token, email, rol y caducidad.</response>
    /// <response code="400">Datos inválidos o reglas de formato incumplidas.</response>
    /// <response code="409">Ya existe una cuenta con ese email o documento.</response>
    [HttpPost("signup")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Signup(
        [FromBody] SignupDto dto,
        CancellationToken ct) {
        logger.LogInformation("Petición de registro de cliente recibida");

        Result<AuthResponseDto, DomainError> resultado = await authService.SignUpAsync(dto, ct);

        return resultado.Match(
            response => CreatedAtAction(nameof(GetMe), response),
            error => error.ToHttpResult<AuthResponseDto>()
        );
    }

    /// <summary>
    /// Devuelve el perfil asociado al token JWT actual.
    /// </summary>
    /// <response code="200">Perfil y rol del usuario.</response>
    /// <response code="401">Token ausente, inválido o sin identificador de usuario.</response>
    /// <response code="403">La cuenta asociada está dada de baja o inactiva.</response>
    /// <response code="404">El usuario del token ya no existe.</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetMe(CancellationToken ct) {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out Guid userId)) {
            return AuthError.CredencialesInvalidas().ToHttpResult<UserDto>();
        }

        Result<UserDto, DomainError> resultado = await authService.GetMeAsync(userId, ct);

        return resultado.Match(
            user => Ok(user),
            error => error.ToHttpResult<UserDto>()
        );
    }
}