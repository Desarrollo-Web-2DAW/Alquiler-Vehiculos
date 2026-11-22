using Microsoft.AspNetCore.SignalR;

namespace Alquiler_Vehiculos.Realtime;

/// <summary>
///     Métodos de extensión para el envío de notificaciones.
/// </summary>
public static class NotificacionesHubExtensions {
    /// <summary>
    ///     Envía una notificación a los trabajadores autorizados.
    /// </summary>
    /// <typeparam name="T">Tipo de los datos enviados.</typeparam>
    /// <param name="hubContext">Contexto del Hub.</param>
    /// <param name="nombreEvento">Nombre del evento.</param>
    /// <param name="datos">Datos asociados al evento.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    public static Task SendToTrabajadoresAsync<T>(this IHubContext<NotificacionesHub> hubContext, string nombreEvento,
        T datos, CancellationToken cancellationToken = default) {
        return hubContext.Clients
            .Group(NotificacionesHub.GrupoTrabajadoresOficina)
            .SendAsync(nombreEvento, datos, cancellationToken);
    }
}