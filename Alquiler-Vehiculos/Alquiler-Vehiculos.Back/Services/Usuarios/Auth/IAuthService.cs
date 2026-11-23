using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Errors.Common;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Usuarios.Auth;

/// <summary>
/// Contrato del servicio de autenticación con ASP.NET Core Identity y JWT.
/// </summary>
public interface IAuthService {
    /// <summary>Inicia sesión con email y contraseña.</summary>
    /// <param name="loginDto">Credenciales de acceso.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Token y datos de la cuenta, o el error de dominio.</returns>
    Task<Result<AuthResponseDto, DomainError>> LoginAsync(LoginDto loginDto, CancellationToken ct = default);

    /// <summary>Da de alta un cliente con su cuenta y devuelve el token.</summary>
    /// <param name="signupDto">Datos de la persona y credenciales.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Token y datos de la cuenta, o el error de dominio.</returns>
    Task<Result<AuthResponseDto, DomainError>> SignUpAsync(SignupDto signupDto, CancellationToken ct = default);

    /// <summary>Devuelve la cuenta del usuario autenticado.</summary>
    /// <param name="userId">ID de la cuenta.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Datos públicos de la cuenta, o el error de dominio.</returns>
    Task<Result<UserDto, DomainError>> GetMeAsync(Guid userId, CancellationToken ct = default);
}
