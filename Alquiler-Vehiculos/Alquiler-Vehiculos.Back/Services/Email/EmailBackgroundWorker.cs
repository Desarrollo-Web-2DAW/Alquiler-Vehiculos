using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Models.Email;
using Microsoft.Extensions.Options;

namespace Alquiler_Vehiculos.Services.Email;

/// <summary>
///     Proceso en segundo plano que envía los correos encolados (outbox).
///     Cada pasada toma los mensajes pendientes cuyo reintento ya vence y
///     trata de enviarlos: si falla, registra el error y programa el próximo
///     intento con backoff exponencial (RF-35); si agota los intentos, queda
///     como <see cref="EstadoEmailOutbox.Fallido" /> para revisión.
///     Un fallo de SMTP nunca se propaga a las peticiones HTTP.
/// </summary>
public class EmailBackgroundWorker(
    IEmailOutboxStore outbox,
    IServiceScopeFactory scopeFactory,
    IOptions<EmailSettings> options,
    ILogger<EmailBackgroundWorker> logger) : BackgroundService {
    private readonly EmailSettings _settings = options.Value;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        logger.LogInformation("Servicio de envío de correos iniciado (cada {Segundos} s)",
            _settings.FrecuenciaProcesoSegundos);

        while (!stoppingToken.IsCancellationRequested) {
            try {
                await ProcesarPendientesAsync(stoppingToken);
            } catch (OperationCanceledException) {
                break;
            } catch (Exception ex) {
                logger.LogError(ex, "Error inesperado procesando la cola de correos");
            }

            try {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _settings.FrecuenciaProcesoSegundos)), stoppingToken);
            } catch (OperationCanceledException) {
                break;
            }
        }

        logger.LogInformation("Servicio de envío de correos detenido");
    }

    private async Task ProcesarPendientesAsync(CancellationToken stoppingToken) {
        var pendientes = await outbox.ObtenerPendientesAsync(_settings.LoteMaximo, stoppingToken);

        foreach (var mensaje in pendientes) {
            try {
                using var scope = scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                await emailService.EnviarAsync(mensaje, stoppingToken);

                mensaje.Estado = EstadoEmailOutbox.Enviado;
                mensaje.FechaEnvio = DateTimeOffset.UtcNow;
                mensaje.UltimoError = null;
                await outbox.ActualizarAsync(mensaje, stoppingToken);

                logger.LogInformation("Correo {Tipo} enviado (clave {Clave}, intento {Intentos})",
                    mensaje.Tipo, mensaje.ClaveIdempotencia, mensaje.Intentos + 1);
            } catch (Exception ex) {
                mensaje.Intentos++;
                mensaje.UltimoError = ex.Message;

                if (mensaje.Intentos >= _settings.MaxIntentos) {
                    mensaje.Estado = EstadoEmailOutbox.Fallido;
                    logger.LogError(ex,
                        "Correo {Tipo} (clave {Clave}) marcado como Fallido tras {Intentos} intentos: queda registrado para revisión",
                        mensaje.Tipo, mensaje.ClaveIdempotencia, mensaje.Intentos);
                } else {
                    var espera = _settings.IntervaloBaseReintentosSegundos * Math.Pow(2, mensaje.Intentos - 1);
                    mensaje.ProximoIntento = DateTimeOffset.UtcNow.AddSeconds(espera);
                    logger.LogWarning(ex,
                        "Fallo enviando el correo {Tipo} (clave {Clave}, intento {Intentos}); próximo intento en {Espera} s",
                        mensaje.Tipo, mensaje.ClaveIdempotencia, mensaje.Intentos, espera);
                }

                await outbox.ActualizarAsync(mensaje, stoppingToken);
            }
        }
    }
}
