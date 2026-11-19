using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Errors.Common;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Usuarios.Auth;

/// <summary>
/// Contrato del servicio de autenticación.
/// </summary>
public interface IAuthService {
    /// <summary>Inicia sesión con email y contraseña.</summary>
    /// <param name="loginDto">Credenciales de acceso.</param>
    /// <returns>Token y datos de la cuenta, o el error de dominio.</returns>
    Task<Result<AuthResponseDto, DomainError>> SignInAsync(LoginDto loginDto);

    /// <summary>Da de alta un cliente con su cuenta y devuelve el token.</summary>
    /// <param name="signupDto">Datos de la persona y credenciales.</param>
    /// <returns>Token y datos de la cuenta, o el error de dominio.</returns>
    Task<Result<AuthResponseDto, DomainError>> SignUpAsync(SignupDto signupDto);

    /// <summary>Devuelve la cuenta del usuario autenticado.</summary>
    /// <param name="userId">ID de la cuenta.</param>
    /// <returns>Datos públicos de la cuenta, o el error de dominio.</returns>
    Task<Result<UserDto, DomainError>> GetMeAsync(Guid userId);
}
