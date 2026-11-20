using Alquiler_Vehiculos.Cache;

namespace Alquiler_Vehiculos.Tests.Unit.Cache;

/// <summary>
/// Almacén falso en memoria que replica la semántica de <see cref="RedisCacheStore"/>
/// (TTL físico, índice de recencia, etiquetas y expulsión LRU) para poder probar
/// la política de la caché de forma unitaria, sin Redis.
/// La validación contra Redis de verdad vive en las pruebas de integración.
/// </summary>
public class FakeCacheStore(TimeProvider clock) : ICacheStore {
    private sealed class Entry(string payload, DateTimeOffset expiresAt) {
        public string Payload { get; set; } = payload;
        public DateTimeOffset ExpiresAt { get; set; } = expiresAt;
    }

    private readonly Dictionary<string, Entry> _values = new();
    private readonly Dictionary<string, double> _scores = new();
    private readonly Dictionary<string, HashSet<string>> _tagToKeys = new();
    private readonly Dictionary<string, HashSet<string>> _keyToTags = new();

    private DateTimeOffset Now => clock.GetUtcNow();

    /// <summary>
    /// Claves que siguen indexadas (para comprobar el estado del índice en pruebas).
    /// </summary>
    public IReadOnlyCollection<string> IndexedKeys => _scores.Keys;

    /// <inheritdoc />
    public Task<string?> GetAsync(string key, CancellationToken ct = default) {
        if (!_values.TryGetValue(key, out var entry) || entry.ExpiresAt <= Now) {
            RemovePhysical(key);
            return Task.FromResult<string?>(null);
        }
        return Task.FromResult<string?>(entry.Payload);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken ct = default) {
        var alive = _values.TryGetValue(key, out var entry) && entry.ExpiresAt > Now;
        return Task.FromResult(alive);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<string>> StoreAsync(string key, string payload, TimeSpan ttl, double score,
        IReadOnlyCollection<string> tags, int capacity, CancellationToken ct = default) {
        // reindexado: si la clave ya existía, se limpia su índice de etiquetas anterior
        RemoveIndex(key, removeValue: false);
        _values[key] = new Entry(payload, Now + ttl);
        _scores[key] = score;
        _keyToTags[key] = [.. tags];
        foreach (var tag in tags) {
            if (!_tagToKeys.TryGetValue(tag, out var keys)) {
                _tagToKeys[tag] = keys = new HashSet<string>();
            }
            keys.Add(key);
        }

        // expulsión LRU mientras se supere la capacidad
        var evicted = new List<string>();
        var over = CachePolicy.EvictionCount(_scores.Count, capacity);
        while (over > 0) {
            var oldest = _scores.OrderBy(kv => kv.Value).Select(kv => kv.Key).FirstOrDefault();
            if (oldest is null) {
                break;
            }
            RemovePhysical(oldest);
            evicted.Add(oldest);
            over--;
        }

        return Task.FromResult<IReadOnlyCollection<string>>(evicted);
    }

    /// <inheritdoc />
    public Task RenewAsync(string key, TimeSpan ttl, double score, CancellationToken ct = default) {
        if (_values.TryGetValue(key, out var entry)) {
            entry.ExpiresAt = Now + ttl;
            _scores[key] = score;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken ct = default) {
        RemovePhysical(key);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveByTagAsync(string tag, CancellationToken ct = default) {
        if (_tagToKeys.TryGetValue(tag, out var keys)) {
            foreach (var key in keys.ToList()) {
                RemovePhysical(key);
            }
            _tagToKeys.Remove(tag);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken ct = default) {
        _values.Clear();
        _scores.Clear();
        _tagToKeys.Clear();
        _keyToTags.Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<string>> PruneAsync(CancellationToken ct = default) {
        // huérfanas del índice: indexadas pero con el valor ya caducado
        var stale = _scores.Keys
            .Where(key => !_values.TryGetValue(key, out var entry) || entry.ExpiresAt <= Now)
            .ToList();
        foreach (var key in stale) {
            RemovePhysical(key);
        }
        return Task.FromResult<IReadOnlyCollection<string>>(stale);
    }

    /// <summary>
    /// Borra el índice (recencia y etiquetas) de una clave, y su valor si se pide.
    /// </summary>
    private void RemoveIndex(string key, bool removeValue) {
        if (_keyToTags.TryGetValue(key, out var tags)) {
            foreach (var tag in tags) {
                if (_tagToKeys.TryGetValue(tag, out var keys)) {
                    keys.Remove(key);
                    if (keys.Count == 0) {
                        _tagToKeys.Remove(tag);
                    }
                }
            }
            _keyToTags.Remove(key);
        }
        _scores.Remove(key);
        if (removeValue) {
            _values.Remove(key);
        }
    }

    /// <summary>
    /// Elimina una entrada y todo su índice, como los scripts Lua de Redis.
    /// </summary>
    private void RemovePhysical(string key) {
        RemoveIndex(key, removeValue: true);
    }
}
