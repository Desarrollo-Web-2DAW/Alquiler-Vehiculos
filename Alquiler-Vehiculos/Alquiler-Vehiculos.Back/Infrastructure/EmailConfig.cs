using Alquiler_Vehiculos.Services.Email;

namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
///     Registro en el contenedor de dependencias del módulo de correo (RF-34 a RF-36).
/// </summary>
public static class EmailConfig {
    /// <summary>
    ///     Configura las notificaciones por correo: plantillas, cola de envío
    ///     (outbox) con reintentos, envío SMTP (o simulado) y el worker en
    ///     segundo plano. Basta una línea en <c>Program.cs</c>.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la aplicación (sección <c>Email</c>).</param>
    public static IServiceCollection AddEmailNotifications(
        this IServiceCollection services, IConfiguration configuration) {
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.Seccion));
        var settings = configuration.GetSection(EmailSettings.Seccion).Get<EmailSettings>() ?? new EmailSettings();

        services.AddSingleton<IEmailOutboxStore, InMemoryEmailOutboxStore>();
        services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();

        if (settings.UseSmtp) {
            services.AddScoped<IEmailService, MailKitEmailService>();
        } else {
            services.AddScoped<IEmailService, MemoryEmailService>();
        }

        services.AddScoped<IEmailNotifier, EmailNotifier>();
        services.AddHostedService<EmailBackgroundWorker>();

        return services;
    }
}
