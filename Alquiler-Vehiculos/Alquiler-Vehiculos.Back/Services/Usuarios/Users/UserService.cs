using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios;
using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Alquiler_Vehiculos.Services.Usuarios.Users;

/// <summary>
/// Servicio de administración de cuentas con ASP.NET Core Identity y patrón Result (RF-46).
/// </summary>
public class UserService(
    UserManager<User> userManager,
    AppDbContextUsuarios db,
    IValidator<CambiarRolDto> cambiarRolValidator,
    ILogger<UserService> logger
) : IUserService {
    private const string RolAdmin = nameof(RolUsuario.Admin);

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UserDetalleDto>, DomainError>> GetAllAsync(
        CancellationToken ct = default) {
        var usuarios = await db.Users
            .AsNoTracking()
            .Include(u => u.Cliente)
            .Include(u => u.Trabajador)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(ct);

        // Una sola consulta para los roles evita N+1 sobre userManager.GetRolesAsync (RNF-02).
        var rolesPorUsuario = await db.UserRoles
            .AsNoTracking()
            .Join(
                db.Roles.AsNoTracking(),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, RoleName = r.Name ?? string.Empty })
            .GroupBy(x => x.UserId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(x => x.RoleName).FirstOrDefault() ?? string.Empty,
                ct);

        var resultado = usuarios
            .Select(u => MapearDetalle(u, rolesPorUsuario.GetValueOrDefault(u.Id, string.Empty)))
            .ToList();

        return Result.Success<IReadOnlyList<UserDetalleDto>, DomainError>(resultado);
    }

    /// <inheritdoc />
    public async Task<Result<UserDetalleDto, DomainError>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default) {
        var user = await BuscarConPerfilAsync(id, ct);
        if (user is null) {
            return Result.Failure<UserDetalleDto, DomainError>(UsuarioError.NotFound(id));
        }

        var roles = await userManager.GetRolesAsync(user);
        return Result.Success<UserDetalleDto, DomainError>(
            MapearDetalle(user, roles.FirstOrDefault() ?? string.Empty));
    }

    /// <inheritdoc />
    public async Task<Result<UserDetalleDto, DomainError>> CambiarRolAsync(
        Guid id,
        CambiarRolDto dto,
        CancellationToken ct = default) {
        var validacion = await cambiarRolValidator.ValidateAsync(dto, ct);
        if (!validacion.IsValid) {
            return Result.Failure<UserDetalleDto, DomainError>(
                UsuarioError.ValidacionConCampos(AgruparErrores(validacion)));
        }

        if (!Enum.TryParse<RolUsuario>(dto.Role.Trim(), ignoreCase: true, out var nuevoRol)
            || !Enum.IsDefined(nuevoRol)) {
            return Result.Failure<UserDetalleDto, DomainError>(UsuarioError.RolInvalido(dto.Role));
        }

        var user = await BuscarConPerfilAsync(id, ct);
        if (user is null) {
            return Result.Failure<UserDetalleDto, DomainError>(UsuarioError.NotFound(id));
        }

        if (!RolCompatibleConPersona(user, nuevoRol)) {
            return Result.Failure<UserDetalleDto, DomainError>(
                UsuarioError.RolIncompatibleConPersona(nuevoRol.ToString(), id));
        }

        var rolesActuales = await userManager.GetRolesAsync(user);
        var nuevoRolNombre = nuevoRol.ToString();

        if (rolesActuales.Contains(RolAdmin) && nuevoRol != RolUsuario.Admin) {
            if (await EsUltimoAdminActivoAsync(user.Id, ct)) {
                return Result.Failure<UserDetalleDto, DomainError>(UsuarioError.UltimoAdmin());
            }
        }

        if (rolesActuales.Count == 1 && rolesActuales[0] == nuevoRolNombre) {
            return Result.Success<UserDetalleDto, DomainError>(MapearDetalle(user, nuevoRolNombre));
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        if (rolesActuales.Count > 0) {
            var removeResult = await userManager.RemoveFromRolesAsync(user, rolesActuales);
            if (!removeResult.Succeeded) {
                await transaccion.RollbackAsync(ct);
                return Result.Failure<UserDetalleDto, DomainError>(
                    UsuarioError.ValidacionConCampos(AgruparErrores(removeResult)));
            }
        }

        var addResult = await userManager.AddToRoleAsync(user, nuevoRolNombre);
        if (!addResult.Succeeded) {
            await transaccion.RollbackAsync(ct);
            return Result.Failure<UserDetalleDto, DomainError>(
                UsuarioError.ValidacionConCampos(AgruparErrores(addResult)));
        }

        user.UpdatedAt = DateTime.UtcNow;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded) {
            await transaccion.RollbackAsync(ct);
            return Result.Failure<UserDetalleDto, DomainError>(
                UsuarioError.ValidacionConCampos(AgruparErrores(updateResult)));
        }

        await transaccion.CommitAsync(ct);

        logger.LogInformation("Rol actualizado para cuenta {UserId} -> {NuevoRol}", user.Id, nuevoRolNombre);
        return Result.Success<UserDetalleDto, DomainError>(MapearDetalle(user, nuevoRolNombre));
    }

    /// <inheritdoc />
    public async Task<UnitResult<DomainError>> DesactivarAsync(
        Guid id,
        CancellationToken ct = default) {
        var user = await BuscarConPerfilAsync(id, ct);
        if (user is null) {
            return UnitResult.Failure<DomainError>(UsuarioError.NotFound(id));
        }

        if (user.IsDeleted) {
            return UnitResult.Success<DomainError>();
        }

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Contains(RolAdmin) && await EsUltimoAdminActivoAsync(user.Id, ct)) {
            return UnitResult.Failure<DomainError>(UsuarioError.UltimoAdmin());
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded) {
            return UnitResult.Failure<DomainError>(
                UsuarioError.ValidacionConCampos(AgruparErrores(updateResult)));
        }

        logger.LogInformation("Cuenta desactivada lógicamente: {UserId}", user.Id);
        return UnitResult.Success<DomainError>();
    }

    /// <inheritdoc />
    public async Task<Result<UserDetalleDto, DomainError>> ReactivarAsync(
        Guid id,
        CancellationToken ct = default) {
        var user = await BuscarConPerfilAsync(id, ct);
        if (user is null) {
            return Result.Failure<UserDetalleDto, DomainError>(UsuarioError.NotFound(id));
        }

        user.IsDeleted = false;
        user.DeletedAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded) {
            return Result.Failure<UserDetalleDto, DomainError>(
                UsuarioError.ValidacionConCampos(AgruparErrores(updateResult)));
        }

        var roles = await userManager.GetRolesAsync(user);
        logger.LogInformation("Cuenta reactivada: {UserId}", user.Id);
        return Result.Success<UserDetalleDto, DomainError>(
            MapearDetalle(user, roles.FirstOrDefault() ?? string.Empty));
    }

    private Task<User?> BuscarConPerfilAsync(Guid id, CancellationToken ct) {
        return userManager.Users
            .Include(u => u.Cliente)
            .Include(u => u.Trabajador)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    private async Task<bool> EsUltimoAdminActivoAsync(Guid userId, CancellationToken ct) {
        var admins = await userManager.GetUsersInRoleAsync(RolAdmin);
        var adminIds = admins.Select(a => a.Id).ToList();

        var adminsActivos = await db.Users
            .AsNoTracking()
            .Where(u => adminIds.Contains(u.Id)
                && !u.IsDeleted
                && (u.Trabajador == null || (u.Trabajador.Activo && !u.Trabajador.IsDeleted)))
            .Select(u => u.Id)
            .ToListAsync(ct);

        return adminsActivos.Count <= 1 && adminsActivos.Contains(userId);
    }

    private static bool RolCompatibleConPersona(User user, RolUsuario nuevoRol) {
        if (user.ClienteId is not null) {
            return nuevoRol == RolUsuario.Cliente;
        }

        if (user.TrabajadorId is not null) {
            return nuevoRol is RolUsuario.Trabajador or RolUsuario.Responsable or RolUsuario.Admin;
        }

        // Cuenta administrativa sin perfil de cliente ni trabajador: solo puede ser Admin.
        return nuevoRol == RolUsuario.Admin;
    }

    private static UserDetalleDto MapearDetalle(User user, string role) {
        return new UserDetalleDto(
            user.Id,
            user.Email ?? string.Empty,
            user.Cliente?.Nombre ?? user.Trabajador?.Nombre ?? string.Empty,
            user.Cliente?.Apellidos ?? user.Trabajador?.Apellidos ?? string.Empty,
            role,
            user.IsDeleted,
            user.DeletedAt,
            user.ClienteId,
            user.TrabajadorId,
            user.CreatedAt);
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
