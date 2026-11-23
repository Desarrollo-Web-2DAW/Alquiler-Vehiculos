using Alquiler_Vehiculos.Realtime;

namespace Alquiler_Vehiculos.Infrastructure;

public static class SignalRExtensions {
    public const string CorsPolicy = "SignalR";

    public static IServiceCollection AddRealtimeSignalR(this IServiceCollection services) {
        services.AddCors(options => {
            options.AddPolicy(CorsPolicy, policy => {
                policy
                    .WithOrigins("http://localhost:5232")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        services.AddSignalR()
            .AddHubOptions<NotificacionesHub>(options => {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 1024 * 4;
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            });

        return services;
    }

    public static IApplicationBuilder MapSignalRHubs(this IApplicationBuilder app) {
        var webApp = (WebApplication)app;
        webApp.MapHub<NotificacionesHub>("/hubs/notificaciones");
        return app;
    }
}