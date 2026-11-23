namespace Alquiler_Vehiculos.Models.Email;

/// <summary>
///     Tipos de notificación por correo electrónico de la aplicación.
///     Cada valor se corresponde con una plantilla HTML de <c>Templates/Email</c>
///     y con un evento de negocio del enunciado.
/// </summary>
public enum TipoNotificacion {
    /// <summary>RF-34: acuse de alta de reserva con detalle, líneas y precio.</summary>
    ReservaCreada,

    /// <summary>RF-34: acuse de pago aprobado con importe y cuatro últimos dígitos.</summary>
    PagoAprobado,

    /// <summary>RF-34 / RF-38: aviso de caducidad por falta de pago.</summary>
    ReservaCaducada,

    /// <summary>RF-34: acuse de cancelación con el detalle de la reserva.</summary>
    ReservaCancelada,

    /// <summary>RF-34: instrucciones de recogida (horario de oficina y datos del vehículo).</summary>
    ReservaRecogida,

    /// <summary>RF-34: resumen del alquiler y coste final tras la devolución.</summary>
    ReservaDevuelta,

    /// <summary>RF-34: aviso de reembolso (anulación de reserva o penalización por no presentarse, CU-13).</summary>
    Reembolso,

    /// <summary>RF-34 / RF-07: aviso de arreglo de taller con reservas afectadas.</summary>
    AfectacionTaller,

    /// <summary>CU-12 (acuerdo de equipo): aviso de avería durante el alquiler.</summary>
    AveriaDuranteAlquiler,

    /// <summary>CU-23 (acuerdo de equipo): aviso de que el vehículo reservado no estará listo.</summary>
    VehiculoNoListo,

    /// <summary>CU-24 (acuerdo de equipo): aviso al cliente afectado por una devolución tardía.</summary>
    DevolucionTardia
}
