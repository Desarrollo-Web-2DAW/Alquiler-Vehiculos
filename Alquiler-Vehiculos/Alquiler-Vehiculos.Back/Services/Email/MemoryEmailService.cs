using System.Collections.Concurrent;
using Alquiler_Vehiculos.Models.Email;

namespace Alquiler_Vehiculos.Services.Email;

/// <summary>
///     Implementación simulada de <see cref="IEmailService" /> para pruebas:
///     no envía nada, registra los mensajes y los guarda en
///     <see cref="Enviados" /> para poder inspeccionarlos.
///     Se activa con <c>Email:UseSmtp = false</c>.
/// </summary>
public class MemoryEmailService(ILogger<MemoryEmailService> logger) : IEmailService {
    private static readonly ConcurrentBag<EmailOutboxMessage> _enviados = new();

    /// <summary>Mensajes registrados por el servicio simulado.</summary>
    public static IReadOnlyList<EmailOutboxMessage> Enviados => _enviados.ToList();

    /// <inheritdoc />
    public Task EnviarAsync(EmailOutboxMessage mensaje, CancellationToken cancellationToken = default) {
        logger.LogInformation("[CORREO SIMULADO] {Tipo} para {Destinatario}: {Asunto}",
            mensaje.Tipo, mensaje.DestinatarioEmail, mensaje.Asunto);
        _enviados.Add(mensaje);
        return Task.CompletedTask;
    }

    /// <summary>Vacía el registro de mensajes (útil entre pruebas).</summary>
    public static void Vaciar() {
        while (!_enviados.IsEmpty) {
            _enviados.TryTake(out _);
        }
    }
}
