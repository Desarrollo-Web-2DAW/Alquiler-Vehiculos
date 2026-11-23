using Alquiler_Vehiculos.Dto.Oficina;
using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Oficina;
using Alquiler_Vehiculos.Models.Usuarios;
using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OficinaEntity = Alquiler_Vehiculos.Models.Oficina.Oficina;

namespace Alquiler_Vehiculos.Services.Oficina;

/// <summary>
/// Servicio de negocio de la oficina única con patrón Result y sincronización transaccional de responsable (RF-11, CU-18, CU-22).
/// </summary>
public class OficinaService(
    AppDbContextUsuarios db,
    UserManager<User> userManager,
    IValidator<UpdateOficinaDto> updateValidator,
    IValidator<CambiarResponsableDto> cambiarResponsableValidator,
    ILogger<OficinaService> logger
) : IOficinaService {
    /// <inheritdoc />
    public async Task<Result<OficinaResponseDto, DomainError>> ObtenerAsync(CancellationToken ct = default) {
        var oficina = await db.Oficinas
            .AsNoTracking()
            .Include(o => o.Responsable)
            .FirstOrDefaultAsync(o => o.Id == OficinaEntity.OficinaUnicaId, ct);

        return oficina is null
            ? Result.Failure<OficinaResponseDto, DomainError>(OficinaError.NoConfigurada())
            : Result.Success<OficinaResponseDto, DomainError>(Mapear(oficina));
    }

    /// <inheritdoc />
    public async Task<Result<OficinaResponseDto, DomainError>> ActualizarAsync(
        UpdateOficinaDto dto,
        CancellationToken ct = default) {
        var validacion = await updateValidator.ValidateAsync(dto, ct);
        if (!validacion.IsValid) {
            return Result.Failure<OficinaResponseDto, DomainError>(
                OficinaError.ValidacionConCampos(AgruparErrores(validacion)));
        }

        var oficina = await db.Oficinas
            .Include(o => o.Responsable)
            .FirstOrDefaultAsync(o => o.Id == OficinaEntity.OficinaUnicaId, ct);

        if (oficina is null) {
            return Result.Failure<OficinaResponseDto, DomainError>(OficinaError.NoConfigurada());
        }

        oficina.Nombre = dto.Nombre.Trim();
        oficina.Direccion = dto.Direccion.Trim();
        oficina.Telefono = dto.Telefono.Trim();
        oficina.Email = dto.Email.Trim().ToLowerInvariant();
        oficina.DiaInicio = dto.DiaInicio;
        oficina.DiaFin = dto.DiaFin;
        oficina.HoraApertura = dto.HoraApertura;
        oficina.HoraCierre = dto.HoraCierre;
        oficina.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Datos y horario de la oficina actualizados");
        return Result.Success<OficinaResponseDto, DomainError>(Mapear(oficina));
    }

    /// <inheritdoc />
    public async Task<Result<OficinaResponseDto, DomainError>> CambiarResponsableAsync(
        CambiarResponsableDto dto,
        CancellationToken ct = default) {
        var validacion = await cambiarResponsableValidator.ValidateAsync(dto, ct);
        if (!validacion.IsValid) {
            return Result.Failure<OficinaResponseDto, DomainError>(
                OficinaError.ValidacionConCampos(AgruparErrores(validacion)));
        }

        var oficina = await db.Oficinas
            .Include(o => o.Responsable)
            .FirstOrDefaultAsync(o => o.Id == OficinaEntity.OficinaUnicaId, ct);

        if (oficina is null) {
            return Result.Failure<OficinaResponseDto, DomainError>(OficinaError.NoConfigurada());
        }

        var nuevo = await db.Trabajadores.FirstOrDefaultAsync(t => t.Id == dto.TrabajadorId, ct);
        if (nuevo is null) {
            return Result.Failure<OficinaResponseDto, DomainError>(
                OficinaError.ResponsableNoEncontrado(dto.TrabajadorId));
        }

        if (!nuevo.Activo || nuevo.IsDeleted) {
            return Result.Failure<OficinaResponseDto, DomainError>(
                OficinaError.ResponsableInactivo(nuevo.Id));
        }

        if (nuevo.Tipo != TipoTrabajador.Operaciones) {
            return Result.Failure<OficinaResponseDto, DomainError>(
                OficinaError.ResponsableDebeSerOperaciones(nuevo.Id));
        }

        if (oficina.ResponsableId == nuevo.Id) {
            return Result.Success<OficinaResponseDto, DomainError>(Mapear(oficina));
        }

        var anteriorId = oficina.ResponsableId;

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        oficina.ResponsableId = nuevo.Id;
        oficina.Responsable = nuevo;
        oficina.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await SincronizarRolResponsableAsync(anteriorId, esNuevoResponsable: false, ct);
        await SincronizarRolResponsableAsync(nuevo.Id, esNuevoResponsable: true, ct);

        await transaccion.CommitAsync(ct);

        logger.LogInformation("Responsable de oficina relevado: {AnteriorId} -> {NuevoId}", anteriorId, nuevo.Id);
        return Result.Success<OficinaResponseDto, DomainError>(Mapear(oficina));
    }

    /// <inheritdoc />
    public Task<bool> EsResponsableAsync(Guid trabajadorId, CancellationToken ct = default) {
        return db.Oficinas
            .AsNoTracking()
            .AnyAsync(o => o.Id == OficinaEntity.OficinaUnicaId && o.ResponsableId == trabajadorId, ct);
    }

    /// <inheritdoc />
    public async Task<bool> EstaAbiertaAsync(DateTime fechaHora, CancellationToken ct = default) {
        var oficina = await db.Oficinas
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == OficinaEntity.OficinaUnicaId, ct);

        return oficina is not null && oficina.EstaAbierta(fechaHora);
    }

    private async Task SincronizarRolResponsableAsync(Guid trabajadorId, bool esNuevoResponsable, CancellationToken ct) {
        var cuenta = await userManager.Users.FirstOrDefaultAsync(u => u.TrabajadorId == trabajadorId, ct);
        if (cuenta is null) return;

        var roles = await userManager.GetRolesAsync(cuenta);
        if (roles.Contains(nameof(RolUsuario.Admin))) return;

        var rolObjetivo = esNuevoResponsable ? nameof(RolUsuario.Responsable) : nameof(RolUsuario.Trabajador);
        if (roles.Count == 1 && roles[0] == rolObjetivo) return;

        if (roles.Count > 0) {
            await userManager.RemoveFromRolesAsync(cuenta, roles);
        }

        await userManager.AddToRoleAsync(cuenta, rolObjetivo);
    }

    private static OficinaResponseDto Mapear(OficinaEntity oficina) => new(
        oficina.Nombre,
        oficina.Direccion,
        oficina.Telefono,
        oficina.Email,
        oficina.DiaInicio,
        oficina.DiaFin,
        oficina.HoraApertura,
        oficina.HoraCierre,
        oficina.HorarioFormateado,
        new ResponsableResumenDto(
            oficina.Responsable.Id,
            oficina.Responsable.Nombre,
            oficina.Responsable.Apellidos,
            oficina.Responsable.Email,
            oficina.Responsable.Telefono),
        oficina.UpdatedAt);

    private static Dictionary<string, string[]> AgruparErrores(ValidationResult resultado) =>
        resultado.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
}
