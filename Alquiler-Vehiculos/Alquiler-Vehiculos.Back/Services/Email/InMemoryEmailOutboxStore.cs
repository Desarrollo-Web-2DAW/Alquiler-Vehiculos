using System.Collections.Concurrent;
using Alquiler_Vehiculos.Models.Email;

namespace Alquiler_Vehiculos.Services.Email;

/// <summary>
///     Cola de envío en memoria, indexada por clave de idempotencia para que
///     un mismo evento nunca se encole dos veces. Sustituible por una
///     implementación EF Core sin tocar el resto del módulo.
/// </summary>
public class InMemoryEmailOutboxStore : IEmailOutboxStore {
    private readonly ConcurrentDictionary<string, EmailOutboxMessage> _mensajes = new();

    /// <inheritdoc />
    public Task<bool> AgregarAsync(EmailOutboxMessage mensaje, CancellationToken cancellationToken = default) {
        var añadido = _mensajes.TryAdd(mensaje.ClaveIdempotencia, mensaje);
        return Task.FromResult(añadido);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<EmailOutboxMessage>> ObtenerPendientesAsync(
        int lote, CancellationToken cancellationToken = default) {
        var ahora = DateTimeOffset.UtcNow;
        var pendientes = _mensajes.Values
            .Where(m => m.Estado == EstadoEmailOutbox.Pendiente &&
                        (m.ProximoIntento is null || m.ProximoIntento <= ahora))
            .OrderBy(m => m.FechaCreacion)
            .Take(lote)
            .ToList();
        return Task.FromResult<IReadOnlyList<EmailOutboxMessage>>(pendientes);
    }

    /// <inheritdoc />
    public Task ActualizarAsync(EmailOutboxMessage mensaje, CancellationToken cancellationToken = default) {
        _mensajes[mensaje.ClaveIdempotencia] = mensaje;
        return Task.CompletedTask;
    }
}
