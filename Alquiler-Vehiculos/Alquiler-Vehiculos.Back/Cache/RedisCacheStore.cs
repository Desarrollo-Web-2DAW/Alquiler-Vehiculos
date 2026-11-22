using Microsoft.EntityFrameworkCore.Storage;
using StackExchange.Redis;

namespace Alquiler_Vehiculos.Cache;

/// <summary>
/// Implementación del almacén de la caché sobre Redis (StackExchange.Redis).
///
/// Espacios de claves bajo el prefijo <c>cache:</c>:
/// - <c>cache:val:{clave}</c>: payload de la entrada, con su TTL físico.
/// - <c>cache:lru</c>: sorted set con el orden de recencia (score = marca de acceso).
/// - <c>cache:tag:{etiqueta}</c>: set de claves marcadas con una etiqueta (invalidación selectiva).
/// - <c>cache:tags:{clave}</c>: set de etiquetas de una clave (para limpiar al expulsar).
///
/// Las operaciones que tocan varios de estos sitios (guardar con expulsión LRU,
/// borrar, invalidar por etiqueta y vaciar) se resuelven con scripts Lua para que
/// sean atómicas, coherente con la concurrencia obligatoria de RF-30.
/// </summary>
public class RedisCacheStore(IConnectionMultiplexer multiplexer) : ICacheStore {
    private const string Prefix = "cache:";
    private const string LruKey = Prefix + "lru";

    private static string ValKey(string key) => Prefix + "val:" + key;

    private IDatabase Db => multiplexer.GetDatabase();

    /// <summary>
    /// Guarda, indexa y expulsa las menos usadas si se supera la capacidad. Atómico.
    /// </summary>
    private const string StoreScript = """
        local prefix = ARGV[1]
        local key = ARGV[2]
        local payload = ARGV[3]
        local ttl = tonumber(ARGV[4])
        local score = tonumber(ARGV[5])
        local capacity = tonumber(ARGV[6])
        local tags = {}
        for i = 7, #ARGV do
            tags[#tags + 1] = ARGV[i]
        end

        local valKey = prefix .. 'val:' .. key
        local tagsKey = prefix .. 'tags:' .. key

        -- si la clave ya existía, se limpia su índice de etiquetas anterior
        local oldTags = redis.call('SMEMBERS', tagsKey)
        for _, t in ipairs(oldTags) do
            redis.call('SREM', prefix .. 'tag:' .. t, key)
        end

        redis.call('SET', valKey, payload, 'EX', ttl)
        redis.call('ZADD', prefix .. 'lru', score, key)
        redis.call('DEL', tagsKey)
        for _, t in ipairs(tags) do
            redis.call('SADD', prefix .. 'tag:' .. t, key)
            redis.call('SADD', tagsKey, t)
        end

        -- expulsión LRU mientras se supere la capacidad
        local evicted = {}
        local over = redis.call('ZCARD', prefix .. 'lru') - capacity
        while over > 0 do
            local oldest = redis.call('ZRANGE', prefix .. 'lru', 0, 0)
            if #oldest == 0 then
                break
            end
            local k = oldest[1]
            redis.call('ZREM', prefix .. 'lru', k)
            redis.call('DEL', prefix .. 'val:' .. k)
            local kTagsKey = prefix .. 'tags:' .. k
            local kTags = redis.call('SMEMBERS', kTagsKey)
            for _, t in ipairs(kTags) do
                redis.call('SREM', prefix .. 'tag:' .. t, k)
            end
            redis.call('DEL', kTagsKey)
            evicted[#evicted + 1] = k
            over = over - 1
        end
        return evicted
        """;

    /// <summary>
    /// Elimina una entrada y todo su índice. Atómico.
    /// </summary>
    private const string RemoveScript = """
        local prefix = ARGV[1]
        local key = ARGV[2]
        local tagsKey = prefix .. 'tags:' .. key
        local kTags = redis.call('SMEMBERS', tagsKey)
        for _, t in ipairs(kTags) do
            redis.call('SREM', prefix .. 'tag:' .. t, key)
        end
        redis.call('DEL', tagsKey)
        redis.call('DEL', prefix .. 'val:' .. key)
        redis.call('ZREM', prefix .. 'lru', key)
        return 1
        """;

    /// <summary>
    /// Invalida todas las entradas de una etiqueta. Atómico.
    /// </summary>
    private const string RemoveByTagScript = """
        local prefix = ARGV[1]
        local tag = ARGV[2]
        local tagKey = prefix .. 'tag:' .. tag
        local members = redis.call('SMEMBERS', tagKey)
        for _, k in ipairs(members) do
            local tagsKey = prefix .. 'tags:' .. k
            local kTags = redis.call('SMEMBERS', tagsKey)
            for _, t in ipairs(kTags) do
                redis.call('SREM', prefix .. 'tag:' .. t, k)
            end
            redis.call('DEL', tagsKey)
            redis.call('DEL', prefix .. 'val:' .. k)
            redis.call('ZREM', prefix .. 'lru', k)
        end
        redis.call('DEL', tagKey)
        return #members
        """;

    /// <summary>
    /// Vacía la caché y todo su índice. Atómico.
    /// </summary>
    private const string ClearScript = """
        local prefix = ARGV[1]
        local members = redis.call('ZRANGE', prefix .. 'lru', 0, -1)
        for _, k in ipairs(members) do
            local tagsKey = prefix .. 'tags:' .. k
            local kTags = redis.call('SMEMBERS', tagsKey)
            for _, t in ipairs(kTags) do
                redis.call('SREM', prefix .. 'tag:' .. t, k)
            end
            redis.call('DEL', tagsKey)
            redis.call('DEL', prefix .. 'val:' .. k)
        end
        redis.call('DEL', prefix .. 'lru')
        return #members
        """;

    /// <inheritdoc />
    public async Task<string?> GetAsync(string key, CancellationToken ct = default) {
        var value = await Db.StringGetAsync(ValKey(key));
        return value.IsNull ? null : value.ToString();
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken ct = default) {
        return Db.KeyExistsAsync(ValKey(key));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<string>> StoreAsync(string key, string payload, TimeSpan ttl, double score,
        IReadOnlyCollection<string> tags, int capacity, CancellationToken ct = default) {
        var args = new List<RedisValue> {
            Prefix, key, payload, Math.Max(1, (int)Math.Ceiling(ttl.TotalSeconds)), score, capacity
        };
        args.AddRange(tags.Select(t => (RedisValue)t));

        var result = await Db.ScriptEvaluateAsync(StoreScript, keys: null, values: args.ToArray());
        var evicted = (RedisResult[]?)result ?? [];
        return evicted.Select(k => k.ToString()).ToArray();
    }

    /// <inheritdoc />
    public async Task RenewAsync(string key, TimeSpan ttl, double score, CancellationToken ct = default) {
        await Task.WhenAll(
            Db.KeyExpireAsync(ValKey(key), ttl),
            Db.SortedSetAddAsync(LruKey, key, score)
        );
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken ct = default) {
        await Db.ScriptEvaluateAsync(RemoveScript, keys: null,
            values: new RedisValue[] { Prefix, key });
    }

    /// <inheritdoc />
    public async Task RemoveByTagAsync(string tag, CancellationToken ct = default) {
        await Db.ScriptEvaluateAsync(RemoveByTagScript, keys: null,
            values: new RedisValue[] { Prefix, tag });
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken ct = default) {
        await Db.ScriptEvaluateAsync(ClearScript, keys: null,
            values: new RedisValue[] { Prefix });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<string>> PruneAsync(CancellationToken ct = default) {
        var db = Db;
        var members = await db.SortedSetRangeByRankAsync(LruKey, 0, -1);
        var pruned = new List<string>();

        foreach (var member in members) {
            var key = member.ToString();
            if (!await db.KeyExistsAsync(ValKey(key))) {
                await RemoveAsync(key, ct);
                pruned.Add(key);
            }
        }

        return pruned;
    }
}
