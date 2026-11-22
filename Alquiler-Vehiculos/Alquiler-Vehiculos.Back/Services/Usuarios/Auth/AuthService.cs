using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Entities;
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
/// Servicio de autenticación con ASP.NET Core Identity, JWT y Patrón Result (RF-12, RF-13, RF-46).
/// El hash (PBKDF2), lockout y roles los gestiona Identity; nunca BCrypt manual.
/// </summary>
public class AuthService(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    AppDbContextUsuarios db,
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
    public async Task<Result<AuthResponseDto, DomainError>> LoginAsync(
        LoginDto loginDto,
        CancellationToken ct = default) {
        var validacion = await loginValidator.ValidateAsync(loginDto, ct);
        if (!validacion.IsValid) {
            return Result.Failure<AuthResponseDto, DomainError>(
                AuthError.ValidacionConCampos(AgruparErrores(validacion)));
        }

        var emailNormalizado = loginDto.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(emailNormalizado);
        if (user is null || user.IsDeleted) {
            logger.LogWarning("Login fallido: credenciales no válidas");
            return Result.Failure<AuthResponseDto, DomainError>(AuthError.CredencialesInvalidas());
        }

        var passwordCorrecta = await signInManager.CheckPasswordSignInAsync(
            user, loginDto.Password, lockoutOnFailure: true);

        if (!passwordCorrecta.Succeeded) {
            if (passwordCorrecta.IsLockedOut) {
                logger.LogWarning("Login bloqueado por intentos fallidos: {UserId}", user.Id);
                return Result.Failure<AuthResponseDto, DomainError>(AuthError.CuentaBloqueada());
            }

            logger.LogWarning("Login fallido: contraseña incorrecta para {UserId}", user.Id);
            return Result.Failure<AuthResponseDto, DomainError>(AuthError.CredencialesInvalidas());
        }

        var cuenta = await userManager.Users
            .Include(u => u.Cliente)
            .Include(u => u.Trabajador)
            .FirstAsync(u => u.Id == user.Id, ct);

        if (!CuentaOperativaActiva(cuenta)) {
            logger.LogWarning("Login rechazado: cuenta inactiva {UserId}", cuenta.Id);
            return Result.Failure<AuthResponseDto, DomainError>(AuthError.CuentaInactiva());
        }

        var roles = await userManager.GetRolesAsync(cuenta);
        if (roles.Count == 0) {
            logger.LogWarning("Login rechazado: cuenta sin rol {UserId}", cuenta.Id);
            return Result.Failure<AuthResponseDto, DomainError>(AuthError.CuentaSinRol());
        }

        logger.LogInformation("Inicio de sesión correcto: {UserId}", cuenta.Id);
        return Result.Success<AuthResponseDto, DomainError>(GenerateJwtToken(cuenta, roles));
    }

    /// <summary>
    /// Alta atómica de cliente + cuenta Identity + rol Cliente.
    /// Si falla cualquier paso, la transacción deshace todo sin dejar clientes huérfanos.
    /// </summary>
    public async Task<Result<AuthResponseDto, DomainError>> SignUpAsync(
        SignupDto signupDto,
        CancellationToken ct = default) {
        var validacion = await signupValidator.ValidateAsync(signupDto, ct);
        if (!validacion.IsValid) {
            return Result.Failure<AuthResponseDto, DomainError>(
                AuthError.ValidacionConCampos(AgruparErrores(validacion)));
        }

        var email = signupDto.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(email) is not null) {
            return Result.Failure<AuthResponseDto, DomainError>(AuthError.EmailExistente(email));
        }

        var dni = Normalizar(signupDto.Dni);
        var nie = Normalizar(signupDto.Nie);
        var pasaporte = Normalizar(signupDto.Pasaporte);

        if (await clienteRepository.DocumentoEnUsoAsync(dni, nie, pasaporte, ct)) {
            var documento = dni ?? nie ?? pasaporte ?? string.Empty;
            return Result.Failure<AuthResponseDto, DomainError>(AuthError.DocumentoExistente(documento));
        }

        // RF-13: con DNI o NIE el permiso de conducir es ese mismo documento.
        var permiso = (dni is null && nie is null)
            ? Normalizar(signupDto.NumeroPermisoConducir)
            : null;

        var cliente = new Cliente {
            Nombre = signupDto.Nombre.Trim(),
            Apellidos = signupDto.Apellidos.Trim(),
            Email = email,
            Telefono = signupDto.Telefono.Trim(),
            FechaNacimiento = signupDto.FechaNacimiento,
            Dni = dni,
            Nie = nie,
            Pasaporte = pasaporte,
            NumeroPermisoConducir = permiso
        };

        var user = new User {
            UserName = email,
            Email = email,
            ClienteId = cliente.Id,
            Cliente = cliente
        };

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        try {
            var creacion = await userManager.CreateAsync(user, signupDto.Password);
            if (!creacion.Succeeded) {
                await transaction.RollbackAsync(ct);

                if (creacion.Errors.Any(e => e.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))) {
                    return Result.Failure<AuthResponseDto, DomainError>(AuthError.EmailExistente(email));
                }

                return Result.Failure<AuthResponseDto, DomainError>(
                    AuthError.ValidacionConCampos(AgruparErrores(creacion)));
            }

            var asignacion = await userManager.AddToRoleAsync(user, RolCliente);
            if (!asignacion.Succeeded) {
                await transaction.RollbackAsync(ct);
                return Result.Failure<AuthResponseDto, DomainError>(
                    AuthError.ValidacionConCampos(AgruparErrores(asignacion)));
            }

            await transaction.CommitAsync(ct);
        } catch (DbUpdateException ex) {
            await transaction.RollbackAsync(ct);
            logger.LogWarning(ex, "Conflicto de unicidad en el alta de cliente");
            var documento = dni ?? nie ?? pasaporte ?? email;
            return Result.Failure<AuthResponseDto, DomainError>(AuthError.DocumentoExistente(documento));
        }

        logger.LogInformation("Cliente dado de alta: {ClienteId} (UserId: {UserId})", cliente.Id, user.Id);
        return Result.Success<AuthResponseDto, DomainError>(
            GenerateJwtToken(user, [RolCliente]));
    }

    /// <summary>
    /// Cuenta del usuario autenticado, verificando que siga activa.
    /// </summary>
    public async Task<Result<UserDto, DomainError>> GetMeAsync(Guid userId, CancellationToken ct = default) {
        var user = await userManager.Users
            .Include(u => u.Cliente)
            .Include(u => u.Trabajador)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null || user.IsDeleted) {
            return Result.Failure<UserDto, DomainError>(UsuarioError.NotFound(userId));
        }

        if (!CuentaOperativaActiva(user)) {
            return Result.Failure<UserDto, DomainError>(AuthError.CuentaInactiva());
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
    /// Una cuenta sin persona asociada es la del admin (siempre operativa si no está borrada).
    /// Si representa a un cliente o trabajador, exige Activo = true e IsDeleted = false.
    /// </summary>
    private static bool CuentaOperativaActiva(User cuenta) {
        var sinPersona = cuenta.Cliente is null && cuenta.Trabajador is null;
        if (sinPersona) return true;

        return cuenta.Cliente is { Activo: true, IsDeleted: false }
            || cuenta.Trabajador is { Activo: true, IsDeleted: false };
    }

    /// <summary>
    /// Construye el JWT con roles y claims operativos (cliente_id, trabajador_id, tipo_trabajador).
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

        if (user.Cliente is not null) {
            claims.Add(new Claim("cliente_id", user.Cliente.Id.ToString()));
        }

        if (user.Trabajador is not null) {
            claims.Add(new Claim("trabajador_id", user.Trabajador.Id.ToString()));
            claims.Add(new Claim("tipo_trabajador", user.Trabajador.Tipo.ToString()));
        }

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

    private static string? Normalizar(string? documento) {
        return string.IsNullOrWhiteSpace(documento) ? null : documento.Trim().ToUpperInvariant();
    }

    private static Dictionary<string, string[]> AgruparErrores(ValidationResult resultado) {
        return resultado.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
    }

    private static Dictionary<string, string[]> AgruparErrores(IdentityResult resultado) {
        return resultado.Errors
            .GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
    }
}
