using Alquiler_Vehiculos.Dto.Taller;
using Alquiler_Vehiculos.Enums.Taller;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Taller;
using Alquiler_Vehiculos.Errors.Usuarios;
using Alquiler_Vehiculos.Models.Taller;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using Alquiler_Vehiculos.Repositories.Taller;
using CSharpFunctionalExtensions;
using FluentValidation;
using Alquiler_Vehiculos.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Alquiler_Vehiculos.Services.Taller;

/// <summary>
/// Servicio de partes de taller con patrón Result (RF-08).
/// El orden de las comprobaciones es: primero quién (permiso del tramitador),
/// luego qué (los datos) y al final las reglas de negocio del parte.
/// </summary>
/// <param name="repository">Almacén de partes.</param>
/// <param name="abrirParteValidator">Reglas de formato de la apertura.</param>
/// <param name="logger">Registro de operaciones del taller.</param>
public class ParteTallerService(
    IParteTallerRepository repository,
    IValidator<AbrirParteDto> abrirParteValidator,
    ILogger<ParteTallerService> logger,
    IHubContext<NotificacionesHub> hubContext
) : IParteTallerService {
    public async Task<Result<ParteTallerDto, DomainError>> AbrirParteAsync(
        Trabajador tramitador, AbrirParteDto dto, CancellationToken ct = default) {
        var permiso = ComprobarTramitador(tramitador);
        if (permiso.IsFailure) {
            return Result.Failure<ParteTallerDto, DomainError>(permiso.Error);
        }

        var validacion = await abrirParteValidator.ValidateAsync(dto, ct);
        if (!validacion.IsValid) {
            var errores = validacion.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<ParteTallerDto, DomainError>(ParteTallerError.ValidacionConCampos(errores));
        }

        var parte = new ParteTaller {
            QuienLoTramita = tramitador.Id,
            VehiculoId = dto.VehiculoId,
            Descripcion = dto.Descripcion
        };

        if (!await repository.AgregarAsync(parte, ct)) {
            logger.LogWarning(
                "Intento de abrir un segundo parte para el vehículo {VehiculoId} (RF-08)",
                dto.VehiculoId);
            return Result.Failure<ParteTallerDto, DomainError>(ParteTallerError.ParteAbiertaExistente(dto.VehiculoId));
        }
        
        await EnviarNotificacionAsync(
            NotificacionEventos.VehiculoEntradaTaller,
            new {
                parte.VehiculoId,
                parte.Descripcion,
                Fecha = parte.Fecha
            });

        logger.LogInformation(
            "Parte {ParteId} abierto para el vehículo {VehiculoId} por el trabajador {TrabajadorId}",
            parte.Id, parte.VehiculoId, tramitador.Id);
        return Result.Success<ParteTallerDto, DomainError>(ParteTallerDto.Desde(parte));
    }

    public async Task<Result<ParteTallerDto, DomainError>> CerrarParteAsync(
        Guid parteId, Trabajador tramitador, CancellationToken ct = default) {
        var permiso = ComprobarTramitador(tramitador);
        if (permiso.IsFailure) {
            return Result.Failure<ParteTallerDto, DomainError>(permiso.Error);
        }

        var parte = await repository.ObtenerPorIdAsync(parteId, ct);
        if (parte is null) {
            return Result.Failure<ParteTallerDto, DomainError>(ParteTallerError.NotFound(parteId));
        }

        if (parte.Estado != EstadoParte.Abierto) {
            logger.LogWarning(
                "Intento de cerrar el parte {ParteId} que ya estaba cerrado (RF-08)",
                parteId);
            return Result.Failure<ParteTallerDto, DomainError>(ParteTallerError.YaCerrada(parteId));
        }

        parte.Estado = EstadoParte.Cerrado;
        await repository.ActualizarAsync(parte, ct);
        
        await EnviarNotificacionAsync(
            NotificacionEventos.VehiculoSalidaTaller,
            new {
                parte.VehiculoId,
                parte.Descripcion,
                Fecha = DateTime.UtcNow
            });

        logger.LogInformation(
            "Parte {ParteId} cerrado para el vehículo {VehiculoId} por el trabajador {TrabajadorId}",
            parte.Id, parte.VehiculoId, tramitador.Id);
        return Result.Success<ParteTallerDto, DomainError>(ParteTallerDto.Desde(parte));
    }

    public async Task<Result<ParteTallerDto, DomainError>> ObtenerPorIdAsync(
        Guid id, CancellationToken ct = default) {
        var parte = await repository.ObtenerPorIdAsync(id, ct);
        if (parte is null) {
            return Result.Failure<ParteTallerDto, DomainError>(ParteTallerError.NotFound(id));
        }
        return Result.Success<ParteTallerDto, DomainError>(ParteTallerDto.Desde(parte));
    }

    public async Task<Result<IReadOnlyList<ParteTallerDto>, DomainError>> ListarAbiertosAsync(
        CancellationToken ct = default) {
        var abiertos = await repository.ObtenerAbiertosAsync(ct);
        return Result.Success<IReadOnlyList<ParteTallerDto>, DomainError>(abiertos.Select(ParteTallerDto.Desde).ToList());
    }

    public async Task<Result<IReadOnlyList<ParteTallerDto>, DomainError>> ListarPorVehiculoAsync(
        string vehiculoId, CancellationToken ct = default) {
        var validacionVehiculo = ComprobarVehiculo(vehiculoId);
        if (validacionVehiculo.IsFailure) {
            return Result.Failure<IReadOnlyList<ParteTallerDto>, DomainError>(validacionVehiculo.Error);
        }

        var historial = await repository.ObtenerPorVehiculoAsync(vehiculoId, ct);
        return Result.Success<IReadOnlyList<ParteTallerDto>, DomainError>(historial.Select(ParteTallerDto.Desde).ToList());
    }

    public async Task<Result<bool, DomainError>> TienePartesAbiertosAsync(
        string vehiculoId, CancellationToken ct = default) {
        var validacionVehiculo = ComprobarVehiculo(vehiculoId);
        if (validacionVehiculo.IsFailure) {
            return Result.Failure<bool, DomainError>(validacionVehiculo.Error);
        }

        var abierto = await repository.ObtenerAbiertoPorVehiculoAsync(vehiculoId, ct);
        return Result.Success<bool, DomainError>(abierto is not null);
    }

    /// <summary>
    /// Comprueba quién tramita: solo un <c>Mecanico</c> activo abre y cierra partes (RF-08, RF-12).
    /// </summary>
    private static Result<bool, DomainError> ComprobarTramitador(Trabajador tramitador) {
        if (tramitador.Tipo != TipoTrabajador.Mecanico) {
            return Result.Failure<bool, DomainError>(ParteTallerError.SoloMecanico(tramitador.Id));
        }
        if (!tramitador.Activo || tramitador.IsDeleted) {
            return Result.Failure<bool, DomainError>(TrabajadorError.TrabajadorInactivo(tramitador.Id));
        }
        return Result.Success<bool, DomainError>(true);
    }

    /// <summary>Comprueba que la consulta traiga un vehículo identificado.</summary>
    private static Result<bool, DomainError> ComprobarVehiculo(string vehiculoId) {
        if (string.IsNullOrWhiteSpace(vehiculoId)) {
            return Result.Failure<bool, DomainError>(
                ParteTallerError.ValidacionConCampos(new Dictionary<string, string[]> {
                    [nameof(vehiculoId)] = ["El vehículo es obligatorio"]
                }));
        }
        return Result.Success<bool, DomainError>(true);
    }
    
    private async Task EnviarNotificacionAsync<T>(
        string nombreEvento,
        T datos) {
        try {
            await hubContext.SendToTrabajadoresAsync(
                nombreEvento,
                datos,
                CancellationToken.None);
        }
        catch (Exception ex) {
            logger.LogError(
                ex,
                "No se pudo enviar la notificación SignalR {NombreEvento}",
                nombreEvento);
        }
    }
}
