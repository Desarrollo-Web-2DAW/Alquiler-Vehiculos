using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Alquiler_Vehiculos.Cache;

/// <summary>
/// Envoltorio de lo que se guarda en la caché: el valor serializado
/// y su deadline absoluto, necesario para el TTL mixto (D2), porque Redis
/// solo tiene TTL absoluto y hay que recordar hasta cuándo se puede renovar.
/// </summary>
internal sealed record CacheEnvelope(string Json, DateTimeOffset Deadline);

/// <summary>
/// Caché LRU con TTL mixto (ventana móvil + vida máxima) sobre Redis.
/// La política (TTL mixto, expulsión LRU e invalidación por etiquetas) vive aquí
/// y en <see cref="CachePolicy"/>; las operaciones crudas van a <see cref="ICacheStore"/>.
/// Es un servicio singleton y seguro para hilos (RNF-01, RF-30).
/// </summary>
public class LruCacheService : ICacheService {
    private readonly ICacheStore _store;
    private readonly CacheOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<LruCacheService> _logger;

    public LruCacheService(ICacheStore store, IOptions<CacheOptions> options, TimeProvider clock, ILogger<LruCacheService> logger) {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(_options.Capacity, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(_options.SlidingExpirationSeconds, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(_options.AbsoluteMaxLifetimeSeconds, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(_options.PruneIntervalSeconds, 1);

        _store = store ?? throw new ArgumentNullException(nameof(store));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) {
        var payload = await _store.GetAsync(key, ct);
        if (payload is null) {
            return default;
        }

        var envelope = JsonSerializer.Deserialize<CacheEnvelope>(payload);
        if (envelope is null) {
            return default;
        }

        var now = _clock.GetUtcNow();
        if (CachePolicy.IsExpired(now, envelope.Deadline)) {
            // caducada por su vida máxima: se retira de golpe, sin esperar a Redis
            await _store.RemoveAsync(key, ct);
            return default;
        }

        // renovación de la ventana móvil, sin rebasar nunca el deadline (TTL mixto)
        var ttl = CachePolicy.EffectiveTtl(now, envelope.Deadline, _options.SlidingExpiration);
        await _store.RenewAsync(key, ttl, ScoreOf(now));

        return JsonSerializer.Deserialize<T>(envelope.Json);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, IEnumerable<string>? tags = null, CancellationToken ct = default) {
        var now = _clock.GetUtcNow();
        var maxLifetime = expiration ?? _options.AbsoluteMaxLifetime;
        if (maxLifetime <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(expiration), "La vida máxima de la entrada debe ser mayor que cero.");
        }

        var deadline = CachePolicy.ComputeDeadline(now, maxLifetime);
        var ttl = CachePolicy.EffectiveTtl(now, deadline, _options.SlidingExpiration);
        var entryTags = (tags ?? [])
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .ToList();

        var envelope = new CacheEnvelope(JsonSerializer.Serialize(value), deadline);
        var evicted = await _store.StoreAsync(key, JsonSerializer.Serialize(envelope), ttl, ScoreOf(now), entryTags, _options.Capacity, ct);

        if (evicted.Count > 0) {
            _logger.LogDebug("Caché: expulsadas {Count} entradas por superar la capacidad {Capacity}", evicted.Count, _options.Capacity);
        }
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken ct = default) {
        return _store.ExistsAsync(key, ct);
    }

    /// <inheritdoc />
    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, IEnumerable<string>? tags = null, CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(factory);

        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) {
            return cached;
        }

        var value = await factory();
        if (value is not null) {
            await SetAsync(key, value, expiration, tags, ct);
        }

        return value;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken ct = default) {
        return _store.RemoveAsync(key, ct);
    }

    /// <inheritdoc />
    public Task RemoveByTagAsync(string tag, CancellationToken ct = default) {
        return _store.RemoveByTagAsync(tag, ct);
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken ct = default) {
        return _store.ClearAsync(ct);
    }

    /// <inheritdoc />
    public async Task<int> PruneAsync(CancellationToken ct = default) {
        var pruned = await _store.PruneAsync(ct);
        if (pruned.Count > 0) {
            _logger.LogDebug("Caché: podadas {Count} entradas huérfanas del índice", pruned.Count);
        }
        return pruned.Count;
    }

    /// <summary>
    /// Marca de recencia de una entrada: la hora actual en milisegundos.
    /// </summary>
    private static double ScoreOf(DateTimeOffset now) => now.ToUnixTimeMilliseconds();
}
