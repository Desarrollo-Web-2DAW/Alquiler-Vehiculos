using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Errors.Auth;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios.Clientes;
using Alquiler_Vehiculos.Repositories.Clientes;
using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Alquiler_Vehiculos.Services.Usuarios.Auth;

/// <summary>
/// Servicio de autenticación con Patrón Result.
/// El hash y la verificación de la contraseña los hace Identity, no BCrypt.
/// </summary>
public class AuthService(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IClienteRepository clienteRepository,
    IOptions<JwtSettings> jwtSettings,
    IValidator<SignupDto> signupValidator,
    IValidator<LoginDto> loginValidator,
    ILogger<AuthService> logger
) : IAuthService {
    private const string RolCliente = nameof(RolUsuario.Cliente);

    /// <summary>
    /// Login por email. El orden importa: primero se comprueba la contraseña
    /// y solo después el estado de la cuenta, para no revelar datos a quien
    /// no conoce las credenciales.
    /// </summary>
    public async Task<Result<AuthResponseDto, DomainError>> SignInAsync(LoginDto loginDto) {
        logger.LogInformation("Intento de inicio de sesión: {Email}", loginDto.Email);

        var validacion = await loginValidator.ValidateAsync(loginDto);
        if (!validacion.IsValid) {
            return Result.Failure<AuthResponseDto, DomainError>(
                AuthError.ValidacionConCampos(AgruparErrores(validacion)));
        }

        var user = await userManager.FindByEmailAsync(loginDto.Email);
        if (user is null || user.IsDeleted) {
            logger.LogWarning("Login fallido: credenciales no válidas para {Email}", loginDto.Email);
            return Result.Failure<AuthResponseDto, DomainError>(AuthError.CredencialesInvalidas());
        }

        var passwordCorrecta = await signInManager.CheckPasswordSignInAsync(
            user, loginDto.Password, lockoutOnFailure: true);

        if (!passwordCorrecta.Succeeded) {
            if (passwordCorrecta.IsLockedOut) {
                logger.LogWarning("Login bloqueado por intentos fallidos: {Email}", loginDto.Email);
                return Result.Failure<AuthResponseDto, DomainError>(AuthError.CuentaBloqueada());
            }

            logger.LogWarning("Login fallido: contraseña incorrecta para {Email}", loginDto.Email);
            return Result.Failure<AuthResponseDto, DomainError>(AuthError.CredencialesInvalidas());
        }

        // UserManager no carga las navegaciones: se traen aquí para poder
        // leer el estado de la persona (Activo/IsDeleted).
        var cuenta = await userManager.Users
            .Include(u => u.Cliente)
            .Include(u => u.Trabajador)
            .FirstAsync(u => u.Id == user.Id);

        // Una cuenta sin persona asociada es la del admin: no tiene estado operativo.
        var sinPersona = cuenta.Cliente is null && cuenta.Trabajador is null;
        var cuentaActiva = cuenta.Cliente is { Activo: true, IsDeleted: false }
                        || cuenta.Trabajador is { Activo: true, IsDeleted: false };

        if (!sinPersona && !cuentaActiva) {
            logger.LogWarning("Login rechazado: cuenta inactiva {Email}", loginDto.Email);
            return Result.Failure<AuthResponseDto, DomainError>(AuthError.CuentaInactiva());
        }

        var roles = await userManager.GetRolesAsync(cuenta);
        if (roles.Count == 0) {
            logger.LogWarning("Login rechazado: cuenta sin rol {Email}", loginDto.Email);
            return Result.Failure<AuthResponseDto, DomainError>(AuthError.CuentaSinRol());
        }

        logger.LogInformation("Inicio de sesión correcto: {UserId}", cuenta.Id);

        return Result.Success<AuthResponseDto, DomainError>(GenerateJwtToken(cuenta, roles));
    }

    /// <summary>
    /// Alta de cliente + cuenta + rol. Identity inserta el cliente y la cuenta
    /// en la misma operación: o quedan los dos, o no queda ninguno.
    /// </summary>
    public async Task<Result<AuthResponseDto, DomainError>> SignUpAsync(SignupDto signupDto) {
        logger.LogInformation("Registro de cliente solicitado: {Email}", signupDto.Email);

        var validacion = await signupValidator.ValidateAsync(signupDto);
        if (!validacion.IsValid) {
            return Result.Failure<AuthResponseDto, DomainError>(
                AuthError.ValidacionConCampos(AgruparErrores(validacion)));
        }

        if (await userManager.FindByEmailAsync(signupDto.Email) is not null) {
            return Result.Failure<AuthResponseDto, DomainError>(
                AuthError.EmailExistente(signupDto.Email));
        }

        var dni = Normalizar(signupDto.Dni);
        var nie = Normalizar(signupDto.Nie);
        var pasaporte = Normalizar(signupDto.Pasaporte);

        if (await clienteRepository.DocumentoEnUsoAsync(dni, nie, pasaporte)) {
            var documento = dni ?? nie ?? pasaporte ?? string.Empty;
            return Result.Failure<AuthResponseDto, DomainError>(AuthError.DocumentoExistente(documento));
        }

        var cliente = new Cliente {
            Nombre = signupDto.Nombre.Trim(),
            Apellidos = signupDto.Apellidos.Trim(),
            Email = signupDto.Email.Trim().ToLowerInvariant(),
            Telefono = signupDto.Telefono.Trim(),
            FechaNacimiento = signupDto.FechaNacimiento,
            Dni = dni,
            Nie = nie,
            Pasaporte = pasaporte,
            NumeroPermisoConducir = Normalizar(signupDto.NumeroPermisoConducir)
        };

        var user = new User {
            UserName = cliente.Email,
            Email = cliente.Email,
            ClienteId = cliente.Id,
            Cliente = cliente
        };

        var creacion = await userManager.CreateAsync(user, signupDto.Password);
        if (!creacion.Succeeded) {
            return Result.Failure<AuthResponseDto, DomainError>(
                AuthError.ValidacionConCampos(AgruparErrores(creacion)));
        }

        var asignacion = await userManager.AddToRoleAsync(user, RolCliente);
        if (!asignacion.Succeeded) {
            // Sin rol la cuenta no sirve: se deshace el alta.
            await userManager.DeleteAsync(user);
            return Result.Failure<AuthResponseDto, DomainError>(
                AuthError.ValidacionConCampos(AgruparErrores(asignacion)));
        }

        logger.LogInformation("Cliente dado de alta: {ClienteId}", cliente.Id);

        return Result.Success<AuthResponseDto, DomainError>(
            GenerateJwtToken(user, new List<string> { RolCliente }));
    }

    /// <summary>
    /// Cuenta del usuario autenticado, con el nombre de su persona.
    /// </summary>
    public async Task<Result<UserDto, DomainError>> GetMeAsync(Guid userId) {
        var user = await userManager.Users
            .Include(u => u.Cliente)
            .Include(u => u.Trabajador)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null || user.IsDeleted) {
            return Result.Failure<UserDto, DomainError>(UsuarioError.NotFound(userId));
        }

        var roles = await userManager.GetRolesAsync(user);

        return Result.Success<UserDto, DomainError>(new UserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.Cliente?.Nombre ?? user.Trabajador?.Nombre ?? string.Empty,
            user.Cliente?.Apellidos ?? user.Trabajador?.Apellidos ?? string.Empty,
            roles.FirstOrDefault() ?? string.Empty));
    }

    /// <summary>
    /// Construye el token y la respuesta de autenticación.
    /// El admin no tiene persona: en ese caso el nombre es su email.
    /// </summary>
    private AuthResponseDto GenerateJwtToken(User user, IList<string> roles) {
        var settings = jwtSettings.Value;
        var expira = DateTime.UtcNow.AddMinutes(settings.ExpirationMinutes);

        var nombreCompleto = user.Cliente is not null
            ? $"{user.Cliente.Nombre} {user.Cliente.Apellidos}".Trim()
            : user.Trabajador is not null
                ? $"{user.Trabajador.Nombre} {user.Trabajador.Apellidos}".Trim()
                : user.Email ?? string.Empty;

        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, nombreCompleto),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Los roles salen de las tablas de Identity, no de una columna User.Rol.
        claims.AddRange(roles.Select(rol => new Claim(ClaimTypes.Role, rol)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expira,
            signingCredentials: credentials);

        return new AuthResponseDto(
            new JwtSecurityTokenHandler().WriteToken(token),
            user.Email ?? string.Empty,
            roles.FirstOrDefault() ?? string.Empty,
            expira);
    }

    /// <summary>Normaliza un documento: sin espacios y en mayúsculas.</summary>
    private static string? Normalizar(string? documento) {
        return string.IsNullOrWhiteSpace(documento) ? null : documento.Trim().ToUpperInvariant();
    }

    /// <summary>Agrupa los errores de FluentValidation por campo.</summary>
    private static Dictionary<string, string[]> AgruparErrores(ValidationResult resultado) {
        return resultado.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
    }

    /// <summary>Agrupa los errores de Identity por código.</summary>
    private static Dictionary<string, string[]> AgruparErrores(IdentityResult resultado) {
        return resultado.Errors
            .GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
    }
}
