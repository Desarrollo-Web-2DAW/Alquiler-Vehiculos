using Alquiler_Vehiculos.Dto.Trabajadores;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Usuarios;
using Alquiler_Vehiculos.Mappers.Trabajadores;
using Alquiler_Vehiculos.Models.Usuarios;
using Alquiler_Vehiculos.Repositories.Trabajadores;
using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Alquiler_Vehiculos.Services.Usuarios.Trabajadores;

public class TrabajadorService(
    ITrabajadorRepository repo,
    UserManager<User> userManager,
    IValidator<CreateTrabajadorDto> createTrabajadorValidator,
    IValidator<UpdateTrabajadorDto> updateTrabajadorValidator,
    ILogger<TrabajadorService> logger
    ) : ITrabajadorService {
    public async Task<IReadOnlyList<TrabajadorResponseDto>> GetAllAsync() {
        logger.LogDebug("Consultando todos los trabajadores");
        var trabajadores = await repo.GetAll();
        return trabajadores.Select(p => p.ToDto()).ToList();
    }

    public async Task<Result<TrabajadorResponseDto, DomainError>> GetByIdAsync(Guid id) {
        logger.LogDebug("Consultando trabajador con id {Id}", id);
        var trabajador = await repo.GetById(id);
        if (trabajador is null) {
            logger.LogDebug("Trabajador con id {Id} no existente", id);
            return Result.Failure<TrabajadorResponseDto, DomainError>(TrabajadorError.NotFound(id));
        }
        var dto = trabajador.ToDto();
        return Result.Success<TrabajadorResponseDto, DomainError>(dto);
    }

    public async Task<Result<TrabajadorResponseDto, DomainError>> CreateAsync(CreateTrabajadorDto dto) {
        logger.LogDebug("Creando un nuevo trabajador");
        var validator = await createTrabajadorValidator.ValidateAsync(dto);
        if (!validator.IsValid) {
            logger.LogDebug("Trabajador no valido");
            return Result.Failure<TrabajadorResponseDto, DomainError>(ValidationError.WithFieldErrors(AgruparErrores(validator)));
        }
        var dni = dto.Dni.Trim().ToUpperInvariant();
        if (await repo.DniEnUso(dni)) {
            logger.LogDebug("Intento de alta de trabajador con DNI duplicado");
            return Result.Failure<TrabajadorResponseDto, DomainError>(
                TrabajadorError.DniExistente(dni));
        }
        var correo = dto.Email.Trim().ToLowerInvariant();
        if (await repo.EmailEnUso(correo) || await userManager.FindByEmailAsync(correo) is not null) {
            logger.LogDebug("Intento de alta de trabajador con email duplicado");
            return Result.Failure<TrabajadorResponseDto, DomainError>(
                TrabajadorError.EmailExistente(correo));
        }
        var trabajador = dto.ToEntity();
        var user = new User {
            UserName = trabajador.Email,
            Email = trabajador.Email,
            TrabajadorId = trabajador.Id,
            Trabajador = trabajador
        };
        var creacion = await userManager.CreateAsync(user, dto.Password);
        if (!creacion.Succeeded) {
            return Result.Failure<TrabajadorResponseDto, DomainError>(
                ValidationError.WithFieldErrors(AgruparErrores(creacion)));
        }
        var asignacion = await userManager.AddToRoleAsync(user, nameof(RolUsuario.Trabajador));
        if (!asignacion.Succeeded) {
            await userManager.DeleteAsync(user);
            return Result.Failure<TrabajadorResponseDto, DomainError>(
                ValidationError.WithFieldErrors(AgruparErrores(asignacion)));
        }
        logger.LogInformation("Trabajador {Id} creado correctamente", trabajador.Id);
        return Result.Success<TrabajadorResponseDto, DomainError>(trabajador.ToDto());
    }

    public async Task<Result<TrabajadorResponseDto, DomainError>> UpdateAsync(Guid id, UpdateTrabajadorDto dto) {
        logger.LogDebug("Actualizando trabajador con id {Id}", id);
        var validator = await updateTrabajadorValidator.ValidateAsync(dto);
        if (!validator.IsValid) {
            logger.LogDebug("No se puede actualizar al trabajador con id {Id}: datos no válidos", id);
            return Result.Failure<TrabajadorResponseDto, DomainError>(ValidationError.WithFieldErrors(AgruparErrores(validator)));
        }
        var trabajador = await repo.GetById(id);
        if (trabajador is null) {
            logger.LogWarning("Actualización fallida: trabajador {Id} no encontrado", id);
            return Result.Failure<TrabajadorResponseDto, DomainError>(TrabajadorError.NotFound(id));
        }
        if (!trabajador.Activo) {
            logger.LogWarning("Actualización rechazada: trabajador {Id} está inactivo", id);
            return Result.Failure<TrabajadorResponseDto, DomainError>(TrabajadorError.TrabajadorInactivo(id));
        }
        var dni = dto.Dni.Trim().ToUpperInvariant();
        if (await repo.DniEnUso(dni, id)) {
            logger.LogDebug("Actualización rechazada para {Id}: DNI duplicado", id);
            return Result.Failure<TrabajadorResponseDto, DomainError>(
                TrabajadorError.DniExistente(dni));
        }
        var correo = dto.Email.Trim().ToLowerInvariant();
        var usuarioConEmail = await userManager.FindByEmailAsync(correo);
        if (await repo.EmailEnUso(correo, id) || (usuarioConEmail is not null && usuarioConEmail.TrabajadorId != id)) {
            logger.LogDebug("Actualización rechazada para {Id}: email duplicado", id);
            return Result.Failure<TrabajadorResponseDto, DomainError>(
                TrabajadorError.EmailExistente(correo));
        }
        if (trabajador.Email != correo) {
            var user = await userManager.FindByEmailAsync(trabajador.Email);
            if (user is not null) {
                user.Email = correo;
                user.UserName = correo;
                var actualizacionUser = await userManager.UpdateAsync(user);
                if (!actualizacionUser.Succeeded) {
                    return Result.Failure<TrabajadorResponseDto, DomainError>(
                        ValidationError.WithFieldErrors(AgruparErrores(actualizacionUser)));
                }
            }
        }

        var actualizado = await repo.Update(id, dto.ToEntity());
        if (actualizado is null) {
            logger.LogWarning("Actualización fallida: trabajador {Id} no encontrado al guardar", id);
            return Result.Failure<TrabajadorResponseDto, DomainError>(TrabajadorError.NotFound(id));
        }

        logger.LogInformation("Trabajador {Id} actualizado correctamente", id);
        return Result.Success<TrabajadorResponseDto, DomainError>(actualizado.ToDto());
    }

    public async Task<UnitResult<DomainError>> DeleteAsync(Guid id) {
        logger.LogInformation("Solicitud de baja lógica para trabajador {Id}", id);

        var trabajador = await repo.GetById(id);
        if (trabajador is null) {
            logger.LogWarning("Baja lógica fallida: trabajador {Id} no encontrado", id);
            return UnitResult.Failure<DomainError>(TrabajadorError.NotFound(id));
        }
        if (await repo.EsResponsableOficinaAsync(id)) {
            logger.LogWarning("Baja lógica rechazada: trabajador {Id} es responsable de la oficina", id);
            return UnitResult.Failure<DomainError>(TrabajadorError.EsResponsableOficina(id));
        }
        if (await repo.TieneReservasVivasAsync(id)) {
            logger.LogWarning("Baja lógica rechazada: trabajador {Id} tiene reservas vivas", id);
            return UnitResult.Failure<DomainError>(TrabajadorError.ConReservasVivas(id));
        }
        var eliminado = await repo.Delete(id);
        if (!eliminado) {
            logger.LogWarning("Baja lógica fallida: trabajador {Id} no encontrado al eliminar", id);
            return UnitResult.Failure<DomainError>(TrabajadorError.NotFound(id));
        }
        logger.LogInformation("Trabajador {Id} dado de baja lógica correctamente", id);
        return UnitResult.Success<DomainError>();
    }

    public async Task<Result<TrabajadorResponseDto, DomainError>> RestoreAsync(Guid id) {
        logger.LogInformation("Restaurando trabajador con id: {Id}", id);
        var trabajador = await repo.Restore(id);
        if (trabajador is null) {
            logger.LogWarning("Restauración fallida: trabajador {Id} no encontrado o no está dado de baja", id);
            return Result.Failure<TrabajadorResponseDto, DomainError>(TrabajadorError.NotFound(id));
        }
        logger.LogInformation("Trabajador {Id} restaurado correctamente", id);
        return Result.Success<TrabajadorResponseDto, DomainError>(trabajador.ToDto());
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