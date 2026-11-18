using Microsoft.Extensions.Options;

namespace Alquiler_Vehiculos.Cache;

/// <summary>
/// Tarea en segundo plano que poda el índice de la caché (D3).
/// El barrido real está en <see cref="ICacheService.PruneAsync"/>, invocable a mano
/// para poder probarlo sin esperar reloj (criterio de RF-40); esta tarea solo
/// lo lanza de forma periódica con el intervalo configurado.
/// </summary>
public class CachePruneService(ICacheService cache, IOptions<CacheOptions> options, ILogger<CachePruneService> logger) : BackgroundService {
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        using var timer = new PeriodicTimer(options.Value.PruneInterval);
        try {
            while (await timer.WaitForNextTickAsync(stoppingToken)) {
                try {
                    var pruned = await cache.PruneAsync(stoppingToken);
                    if (pruned > 0) {
                        logger.LogDebug("Caché: tarea de poda ha limpiado {Count} entradas", pruned);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                    break;
                }
                catch (Exception ex) {
                    // la poda nunca debe tumbar la aplicación: se registra y se reintenta en el próximo tick
                    logger.LogError(ex, "Caché: error en la tarea de poda");
                }
            }
        }
        catch (OperationCanceledException) {
            // apagado normal de la tarea
        }
    }
}
