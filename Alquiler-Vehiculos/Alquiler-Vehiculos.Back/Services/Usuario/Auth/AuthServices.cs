using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Dto.Usuario;
using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Usuarios;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Alquiler_Vehiculos.Services.Usuario.Auth;

public class AuthServices(
    AppDbContextUsuarios contextUsuarios,
    UserManager<User> userManager,
    SignInManager<User> signUser,
    IEmailMesasager emailService,
    IOptions<JwtSettings> jwtSettings,
    ILogger<AuthServices> logger
) : IAuthService {
    public async Task<Result<AuthResponseDto, DomainError>> Login(LoginDto loginDto) {
        logger.LogInformation("Intentando loguear al usuario con email: {Email}", loginDto.Email);
        
        var user = await userManager.FindByEmailAsync(loginDto.Email);
        if (user is null) {
            logger.LogWarning("Login fallido: usuario con email {Email} no encontrado", loginDto.Email);
            return Result.Failure<AuthResponseDto, DomainError>(UnauthorizedError.InvalidCredentials());
        }
        var result = await signUser.CheckPasswordSignInAsync(user, loginDto.Password, false);
        if (!result.Succeeded) {
            logger.LogWarning("Login fallido: contraseña incorrecta para el usuario con email {Email}", loginDto.Email);
            return Result.Failure<AuthResponseDto, DomainError>(UnauthorizedError.InvalidCredentials());
        }

        var role = (await userManager.GetRolesAsync(user));
        if (role.Count == 0) {
            logger.LogWarning("Login fallido: usuario con email {Email} no tiene roles asignados", loginDto.Email);
            return Result.Failure<AuthResponseDto, DomainError>(UnauthorizedError.InvalidCredentials());
        }
    }

    public Task<Result<AuthResponseDto, DomainError>> Signup(SignupDto signupDto) {
        throw new NotImplementedException();
    }

    public Task<Result<UserDto, DomainError>> GetMe(long userId) {
        throw new NotImplementedException();
    }
    
    private AuthResponseDto GenerateJwtToken(User user, string role) {
        // Implementación de generación de token JWT
        var settings = jwtSettings.Value;
        var expires = DateTime.UtcNow.AddMinutes(settings.ExpirationMinutes);
        var nombreCompleto = user.Cliente is not null
            ? $"{user.Cliente.Nombre} {user.Cliente.Apellidos}".Trim()
            : $"{user.Trabajador!.Nombre} {user.Trabajador!.Apellidos}".Trim();

        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, nombreCompleto)
        };
        
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);
        
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        
        return new AuthResponseDto(tokenString, user.Email ?? "", role, expires);
    }
}