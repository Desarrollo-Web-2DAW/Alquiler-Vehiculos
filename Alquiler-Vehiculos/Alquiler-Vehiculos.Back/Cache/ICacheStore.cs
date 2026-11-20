namespace Alquiler_Vehiculos.Cache;

/// <summary>
/// Operaciones crudas del almacén que usa la caché (Redis).
/// Aísla la política de la caché (TTL mixto, LRU, etiquetas) del cliente concreto
/// y permite probar esa política con un almacén falso en las pruebas unitarias,
/// dejando para las pruebas de integración la validación contra Redis de verdad.
/// </summary>
public interface ICacheStore {
    /// <summary>
    /// Devuelve el payload guardado para una clave, o null si no existe
    /// (o su TTL físico ya la caducó).
    /// </summary>
    /// <param name="key">Clave lógica.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<string?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Indica si la clave existe físicamente en el almacén.
    /// </summary>
    /// <param name="key">Clave lógica.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Guarda la entrada, la indexa en el orden de recencia y en sus etiquetas
    /// y, si se supera la capacidad, expulsa las menos usadas (LRU).
    /// En Redis esta operación es atómica (script Lua).
    /// </summary>
    /// <param name="key">Clave lógica.</param>
    /// <param name="payload">Payload serializado.</param>
    /// <param name="ttl">TTL físico a aplicar.</param>
    /// <param name="score">Marca de recencia (mayor = más reciente).</param>
    /// <param name="tags">Etiquetas de invalidación de la entrada.</param>
    /// <param name="capacity">Capacidad máxima de la caché.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Claves expulsadas por la política LRU.</returns>
    Task<IReadOnlyCollection<string>> StoreAsync(string key, string payload, TimeSpan ttl, double score, IReadOnlyCollection<string> tags, int capacity, CancellationToken ct = default);

    /// <summary>
    /// Renueva el TTL físico de una entrada y su marca de recencia.
    /// </summary>
    /// <param name="key">Clave lógica.</param>
    /// <param name="ttl">Nuevo TTL físico.</param>
    /// <param name="score">Nueva marca de recencia.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task RenewAsync(string key, TimeSpan ttl, double score, CancellationToken ct = default);

    /// <summary>
    /// Elimina una entrada y todo su índice (recencia y etiquetas).
    /// </summary>
    /// <param name="key">Clave lógica.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Elimina todas las entradas marcadas con la etiqueta y la propia etiqueta.
    /// </summary>
    /// <param name="tag">Etiqueta.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task RemoveByTagAsync(string tag, CancellationToken ct = default);

    /// <summary>
    /// Vacía la caché y su índice de recencia y etiquetas.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    Task ClearAsync(CancellationToken ct = default);

    /// <summary>
    /// Elimina del índice las entradas cuyo valor ya caducó (huérfanas del índice).
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Claves podadas.</returns>
    Task<IReadOnlyCollection<string>> PruneAsync(CancellationToken ct = default);
}
