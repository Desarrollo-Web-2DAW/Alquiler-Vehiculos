using Alquiler_Vehiculos.Models.Email;

namespace Alquiler_Vehiculos.Services.Email;

/// <summary>
///     Almacén de la cola de envío de correos (patrón outbox).
///     La implementación en memoria sirve mientras no exista la base de datos
///     transaccional del proyecto; al pasar a EF Core solo cambia esta
///     implementación, no el resto del módulo.
/// </summary>
public interface IEmailOutboxStore {
    /// <summary>
    ///     Añade un mensaje a la cola. Devuelve <c>false</c> si ya existe uno
    ///     con la misma <see cref="EmailOutboxMessage.ClaveIdempotencia" />,
    ///     lo que garantiza exactamente un correo por evento (CU-17).
    /// </summary>
    /// <param name="mensaje">Mensaje a encolar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns><c>true</c> si se añadió; <c>false</c> si era duplicado.</returns>
    Task<bool> AgregarAsync(EmailOutboxMessage mensaje, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Devuelve los mensajes pendientes cuyo próximo intento ya vence,
    ///     ordenados por fecha de creación.
    /// </summary>
    /// <param name="lote">Número máximo de mensajes por pasada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<IReadOnlyList<EmailOutboxMessage>> ObtenerPendientesAsync(int lote, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Actualiza el estado de un mensaje ya encolado (enviado, reintento o fallo).
    /// </summary>
    /// <param name="mensaje">Mensaje con el estado actualizado.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task ActualizarAsync(EmailOutboxMessage mensaje, CancellationToken cancellationToken = default);
}
