using Alquiler_Vehiculos.Dto.Email;

namespace Alquiler_Vehiculos.Services.Email;

/// <summary>
///     Contrato de las notificaciones por correo al cliente (RF-34).
///     Los servicios de negocio (reserva, pago, taller y tareas programadas)
///     llaman a un método por evento; el servicio encola el correo y nunca
///     lanza excepción: un fallo de correo no rompe la operación (RF-35).
/// </summary>
public interface IEmailNotifier {
    /// <summary>RF-34: acuse de alta de reserva.</summary>
    Task NotificarReservaCreadaAsync(ReservaCreadaEmail datos, CancellationToken cancellationToken = default);

    /// <summary>RF-34: acuse de pago aprobado.</summary>
    Task NotificarPagoAprobadoAsync(PagoAprobadoEmail datos, CancellationToken cancellationToken = default);

    /// <summary>RF-34 / RF-38: aviso de caducidad por falta de pago.</summary>
    Task NotificarReservaCaducadaAsync(ReservaCaducadaEmail datos, CancellationToken cancellationToken = default);

    /// <summary>RF-34: acuse de cancelación.</summary>
    Task NotificarReservaCanceladaAsync(ReservaCanceladaEmail datos, CancellationToken cancellationToken = default);

    /// <summary>RF-34: instrucciones de recogida.</summary>
    Task NotificarReservaRecogidaAsync(ReservaRecogidaEmail datos, CancellationToken cancellationToken = default);

    /// <summary>RF-34: resumen del alquiler y coste final.</summary>
    Task NotificarReservaDevueltaAsync(ReservaDevueltaEmail datos, CancellationToken cancellationToken = default);

    /// <summary>RF-34: aviso de reembolso (anulación o penalización por no presentado).</summary>
    Task NotificarReembolsoAsync(ReembolsoEmail datos, CancellationToken cancellationToken = default);

    /// <summary>RF-34 / RF-07: aviso de arreglo de taller con reservas afectadas.</summary>
    Task NotificarAfectacionTallerAsync(AfectacionTallerEmail datos, CancellationToken cancellationToken = default);

    /// <summary>CU-12: aviso de avería durante el alquiler.</summary>
    Task NotificarAveriaDuranteAlquilerAsync(AveriaDuranteAlquilerEmail datos, CancellationToken cancellationToken = default);

    /// <summary>CU-23: aviso de vehículo no listo para la recogida.</summary>
    Task NotificarVehiculoNoListoAsync(VehiculoNoListoEmail datos, CancellationToken cancellationToken = default);

    /// <summary>CU-24: aviso al cliente afectado por una devolución tardía.</summary>
    Task NotificarDevolucionTardiaAsync(DevolucionTardiaEmail datos, CancellationToken cancellationToken = default);
}
