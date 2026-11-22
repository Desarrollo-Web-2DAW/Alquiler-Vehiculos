using Alquiler_Vehiculos.Dto.Email;
using Alquiler_Vehiculos.Models.Email;

namespace Alquiler_Vehiculos.Services.Email;

/// <summary>
///     Implementación de <see cref="IEmailNotifier" />.
///     Renderiza la plantilla del evento y la encola en la cola de envío
///     (outbox). Nunca lanza excepción: si algo falla, se registra y la
///     operación de negocio continúa (RF-35).
/// </summary>
public class EmailNotifier(
    IEmailTemplateRenderer renderizador,
    IEmailOutboxStore outbox,
    ILogger<EmailNotifier> logger) : IEmailNotifier {
    /// <inheritdoc />
    public Task NotificarReservaCreadaAsync(ReservaCreadaEmail datos, CancellationToken cancellationToken = default)
        => EncolarAsync(TipoNotificacion.ReservaCreada, datos, cancellationToken);

    /// <inheritdoc />
    public Task NotificarPagoAprobadoAsync(PagoAprobadoEmail datos, CancellationToken cancellationToken = default)
        => EncolarAsync(TipoNotificacion.PagoAprobado, datos, cancellationToken);

    /// <inheritdoc />
    public Task NotificarReservaCaducadaAsync(ReservaCaducadaEmail datos, CancellationToken cancellationToken = default)
        => EncolarAsync(TipoNotificacion.ReservaCaducada, datos, cancellationToken);

    /// <inheritdoc />
    public Task NotificarReservaCanceladaAsync(ReservaCanceladaEmail datos, CancellationToken cancellationToken = default)
        => EncolarAsync(TipoNotificacion.ReservaCancelada, datos, cancellationToken);

    /// <inheritdoc />
    public Task NotificarReservaRecogidaAsync(ReservaRecogidaEmail datos, CancellationToken cancellationToken = default)
        => EncolarAsync(TipoNotificacion.ReservaRecogida, datos, cancellationToken);

    /// <inheritdoc />
    public Task NotificarReservaDevueltaAsync(ReservaDevueltaEmail datos, CancellationToken cancellationToken = default)
        => EncolarAsync(TipoNotificacion.ReservaDevuelta, datos, cancellationToken);

    /// <inheritdoc />
    public Task NotificarReembolsoAsync(ReembolsoEmail datos, CancellationToken cancellationToken = default)
        => EncolarAsync(TipoNotificacion.Reembolso, datos, cancellationToken);

    /// <inheritdoc />
    public Task NotificarAfectacionTallerAsync(AfectacionTallerEmail datos, CancellationToken cancellationToken = default)
        => EncolarAsync(TipoNotificacion.AfectacionTaller, datos, cancellationToken);

    /// <inheritdoc />
    public Task NotificarAveriaDuranteAlquilerAsync(AveriaDuranteAlquilerEmail datos, CancellationToken cancellationToken = default)
        => EncolarAsync(TipoNotificacion.AveriaDuranteAlquiler, datos, cancellationToken);

    /// <inheritdoc />
    public Task NotificarVehiculoNoListoAsync(VehiculoNoListoEmail datos, CancellationToken cancellationToken = default)
        => EncolarAsync(TipoNotificacion.VehiculoNoListo, datos, cancellationToken);

    /// <inheritdoc />
    public Task NotificarDevolucionTardiaAsync(DevolucionTardiaEmail datos, CancellationToken cancellationToken = default)
        => EncolarAsync(TipoNotificacion.DevolucionTardia, datos, cancellationToken);

    private async Task EncolarAsync(TipoNotificacion tipo, NotificacionEmailBase datos, CancellationToken cancellationToken) {
        try {
            var cuerpo = await renderizador.RenderizarAsync(tipo, datos, cancellationToken);
            var mensaje = new EmailOutboxMessage {
                Tipo = tipo,
                ClaveIdempotencia = datos.ClaveIdempotencia ?? $"{tipo}:{datos.CodigoReferencia}",
                DestinatarioEmail = datos.ClienteEmail,
                DestinatarioNombre = datos.ClienteNombre,
                Asunto = datos.Asunto,
                CuerpoHtml = cuerpo
            };

            var añadido = await outbox.AgregarAsync(mensaje, cancellationToken);
            if (añadido) {
                logger.LogInformation("Correo {Tipo} encolado para la reserva {Codigo}",
                    tipo, datos.CodigoReferencia);
            } else {
                logger.LogInformation(
                    "Correo {Tipo} de la reserva {Codigo} ya estaba encolado: se omite para no duplicar (CU-17)",
                    tipo, datos.CodigoReferencia);
            }
        } catch (Exception ex) {
            // RF-35: el correo nunca bloquea ni rompe la operación de negocio.
            logger.LogError(ex, "No se pudo encolar el correo {Tipo} de la reserva {Codigo}; la operación continúa",
                tipo, datos.CodigoReferencia);
        }
    }
}
