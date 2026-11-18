using Alquiler_Vehiculos.Models.Email;

namespace Alquiler_Vehiculos.Services.Email;

/// <summary>
///     Contrato del envío físico de correos (SMTP).
///     El envío nunca se llama directamente desde el negocio: los mensajes
///     pasan por la cola de envío (outbox) y los procesa
///     <see cref="EmailBackgroundWorker" /> en segundo plano (RF-35).
/// </summary>
public interface IEmailService {
    /// <summary>
    ///     Envía un mensaje ya renderizado a través del servidor de correo.
    ///     Lanza excepción si el servidor no está disponible: el worker la
    ///     captura, la registra y programa el reintento.
    /// </summary>
    /// <param name="mensaje">Mensaje de la cola de envío.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task EnviarAsync(EmailOutboxMessage mensaje, CancellationToken cancellationToken = default);
}
