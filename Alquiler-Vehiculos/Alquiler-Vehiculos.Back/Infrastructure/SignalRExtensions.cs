using Alquiler_Vehiculos.Realtime;

namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
///     Configuración de SignalR.
/// </summary>
public static class SignalRExtensions {
    /// <summary>
    ///     Configura el Hub de notificaciones de SignalR.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <returns>IServiceCollection.</returns>
    public static IServiceCollection AddRealtimeSignalR(this IServiceCollection services) {
        services.AddSignalR()
            .AddHubOptions<NotificacionesHub>(options => {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 1024 * 4;
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            });

        return services;
    }

    /// <summary>
    ///     Mapea el endpoint del Hub de notificaciones.
    /// </summary>
    /// <param name="app">Application builder.</param>
    /// <returns>Application builder.</returns>
    public static IApplicationBuilder MapSignalRHubs(this IApplicationBuilder app) {
        var webApp = (WebApplication)app;

        webApp.MapHub<NotificacionesHub>("/hubs/notificaciones");

        return app;
    }
}