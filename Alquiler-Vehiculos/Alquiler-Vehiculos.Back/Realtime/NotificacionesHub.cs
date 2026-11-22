using Alquiler_Vehiculos.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Alquiler_Vehiculos.Realtime;

/// <summary>
///     Hub encargado de gestionar las conexiones de los usuarios
///     autorizados para recibir notificaciones en tiempo real.
/// </summary>
[Authorize(Policy = PolicyNames.Trabajador)]
public class NotificacionesHub : Hub {
    /// <summary>
    ///     Grupo que agrupa las conexiones autorizadas para
    ///     recibir las notificaciones del sistema.
    /// </summary>
    public const string GrupoTrabajadoresOficina = "trabajadores-oficina";

    /// <summary>
    ///     Añade la conexión al grupo de trabajadores autorizados.
    /// </summary>
    public override async Task OnConnectedAsync() {
        await Groups.AddToGroupAsync(Context.ConnectionId, GrupoTrabajadoresOficina);

        await base.OnConnectedAsync();
    }
}