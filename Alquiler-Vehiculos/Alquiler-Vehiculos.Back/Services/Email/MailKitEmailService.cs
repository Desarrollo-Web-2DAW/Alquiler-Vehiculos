using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Models.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Alquiler_Vehiculos.Services.Email;

/// <summary>
///     Envío real de correos con MailKit (SMTP).
///     El cliente SMTP se inyecta opcionalmente para poder probar la clase
///     sin servidor real (misma técnica que el proyecto de referencia).
/// </summary>
public class MailKitEmailService(
    IOptions<EmailSettings> options,
    ILogger<MailKitEmailService> logger,
    ISmtpClient? smtpClient = null) : IEmailService {
    private readonly EmailSettings _settings = options.Value;

    /// <inheritdoc />
    public async Task EnviarAsync(EmailOutboxMessage mensaje, CancellationToken cancellationToken = default) {
        var mimeMessage = new MimeMessage {
            Subject = mensaje.Asunto,
            Body = new TextPart(TextFormat.Html) { Text = mensaje.CuerpoHtml }
        };
        mimeMessage.From.Add(new MailboxAddress(_settings.FromName, _settings.From));
        mimeMessage.To.Add(new MailboxAddress(mensaje.DestinatarioNombre, mensaje.DestinatarioEmail));

        try {
            using var client = smtpClient ?? new SmtpClient();

            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                ObtenerSecureSocket(_settings.Security),
                cancellationToken);

            // Mailpit (desarrollo) no pide autenticación; el servidor de producción sí.
            if (!string.IsNullOrEmpty(_settings.Username)) {
                await client.AuthenticateAsync(_settings.Username, _settings.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            logger.LogInformation("Correo {Tipo} entregado al servidor SMTP (clave {Clave})",
                mensaje.Tipo, mensaje.ClaveIdempotencia);
        } catch (Exception ex) {
            logger.LogWarning(ex, "Fallo de SMTP enviando el correo {Tipo} (clave {Clave})",
                mensaje.Tipo, mensaje.ClaveIdempotencia);
            throw;
        }
    }

    private static SecureSocketOptions ObtenerSecureSocket(string security) {
        return security.ToLowerInvariant() switch {
            "ssl" => SecureSocketOptions.SslOnConnect,
            "tls" => SecureSocketOptions.StartTls,
            "none" => SecureSocketOptions.None,
            _ => SecureSocketOptions.Auto
        };
    }
}
