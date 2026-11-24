using Alquiler_Vehiculos.Config;
using FluentValidation;

namespace Alquiler_Vehiculos.Validators.Reservas;

/// <summary>
/// Rango de fechas de recogida y devolución a validar (RF-19, CU-25).
/// </summary>
public record RangoFechasReserva(DateTime FechaRecogida, DateTime FechaDevolucion);

/// <summary>
/// Valida reglas de fechas de reserva (RF-19, CU-25):
/// recogida no en el pasado, mínimo 1 día y máximo 30 días de fecha a fecha.
/// </summary>
public class FechasReservaValidator : AbstractValidator<RangoFechasReserva> {
    public FechasReservaValidator(TimeProvider? timeProvider = null) {
        var reloj = timeProvider ?? TimeProvider.System;

        RuleFor(x => x.FechaRecogida)
            .Must(recogida => recogida >= reloj.GetUtcNow().UtcDateTime)
            .WithMessage("La fecha de recogida no puede estar en el pasado");

        RuleFor(x => x)
            .Must(x => CalcularDias(x.FechaRecogida, x.FechaDevolucion) >= ReservasConstantes.DuracionMinimoDias)
            .WithName("fechas")
            .WithMessage($"La devolución debe ser al menos {ReservasConstantes.DuracionMinimoDias} día después de la recogida");

        RuleFor(x => x)
            .Must(x => CalcularDias(x.FechaRecogida, x.FechaDevolucion) <= ReservasConstantes.DuracionMaximoDias)
            .WithName("fechas")
            .WithMessage($"La duración máxima del alquiler es de {ReservasConstantes.DuracionMaximoDias} días");
    }

    /// <summary>
    /// Calcula los días de alquiler de fecha a fecha (RF-19: del 12 al 15 son 3 días).
    /// </summary>
    public static int CalcularDias(DateTime fechaRecogida, DateTime fechaDevolucion) =>
        (fechaDevolucion.Date - fechaRecogida.Date).Days;
}