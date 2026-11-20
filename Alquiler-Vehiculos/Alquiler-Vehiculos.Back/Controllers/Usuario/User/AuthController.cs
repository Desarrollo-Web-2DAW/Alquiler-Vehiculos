using System.Security.Claims;
using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Extensions;
using Alquiler_Vehiculos.Services.Usuarios.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alquiler_Vehiculos.Controllers.Usuario.User;



[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase {
    /// <summary>Inicia sesión y devuelve el token JWT.</summary>
    /// <param name="loginDto">Email y contraseña.</param>
    /// <returns>200 con el token, o el error de dominio ya mapeado a HTTP.</returns>
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto) {
        var result = await authService.Login(loginDto);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult<AuthResponseDto>();
    }

    
    /// <summary>
    /// Registra un usuario con rol USER e inicia sesión devolviendo un JWT.
    /// </summary>
    /// <remarks>La contraseña debe tener al menos 8 caracteres e incluir mayúscula, minúscula, número y símbolo.</remarks>
    /// <response code="200">Usuario creado; devuelve token, email, rol y caducidad.</response>
    /// <response code="400">JSON inválido o campos requeridos ausentes.</response>
    /// <response code="409">Ya existe una cuenta con ese email.</response>
    /// <response code="422">La contraseña no cumple las reglas de Identity u otro requisito de negocio.</response>
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [HttpPost("signup")]
    public async Task<ActionResult<AuthResponseDto>> Signup([FromBody] SignupDto dto) {
        var result = await authService.SignUpAsync(dto);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult<AuthResponseDto>();
    }
    
    
    /// <summary>
    /// Devuelve el perfil asociado al token JWT actual.
    /// </summary>
    /// <response code="200">Perfil y rol del usuario.</response>
    /// <response code="401">Token ausente, inválido o sin identificador de usuario.</response>
    /// <response code="404">El usuario del token ya no existe.</response>
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe() {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!Guid.TryParse(userIdClaim, out Guid userId)) {
            return Unauthorized(new ApiErrorResponse {
                Message = "Token inválido o usuario no identificado"
            });
        }

        var result = await authService.GetMeAsync(userId);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult<UserDto>();
    }
}