namespace Alquiler_Vehiculos.Cache;

/// <summary>
/// Contrato de la caché LRU con TTL mixto.
/// Sigue el esquema del <c>ICacheService</c> del material del curso
/// (GetAsync, SetAsync, RemoveAsync, ExistsAsync, GetOrSetAsync) y añade
/// las operaciones propias de esta tarea: invalidación selectiva por etiquetas
/// (D4, RNF-02 y CU-15) y poda del índice (D3).
/// </summary>
public interface ICacheService {
    /// <summary>
    /// Obtiene un valor de la caché. La lectura renueva la ventana móvil,
    /// pero nunca más allá de la vida máxima de la entrada (TTL mixto).
    /// </summary>
    /// <typeparam name="T">Tipo del valor.</typeparam>
    /// <param name="key">Clave.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El valor o default si no está o está caducado.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// Guarda un valor en la caché con su vida máxima y sus etiquetas de invalidación.
    /// Si la caché supera su capacidad se expulsa la entrada menos usada (LRU).
    /// </summary>
    /// <typeparam name="T">Tipo del valor.</typeparam>
    /// <param name="key">Clave.</param>
    /// <param name="value">Valor a guardar.</param>
    /// <param name="expiration">Vida máxima de la entrada; si es null, la de las opciones.</param>
    /// <param name="tags">Etiquetas por las que se podrá invalidar esta entrada.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, IEnumerable<string>? tags = null, CancellationToken ct = default);

    /// <summary>
    /// Indica si una clave existe físicamente en la caché.
    /// Como el TTL de Redis nunca supera el deadline de la entrada,
    /// existir implica estar viva.
    /// </summary>
    /// <param name="key">Clave.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>true si la clave existe.</returns>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Obtiene un valor de la caché y, si no está, lo calcula con la fábrica y lo guarda.
    /// </summary>
    /// <typeparam name="T">Tipo del valor.</typeparam>
    /// <param name="key">Clave.</param>
    /// <param name="factory">Fábrica del valor en caso de fallo de caché.</param>
    /// <param name="expiration">Vida máxima de la entrada; si es null, la de las opciones.</param>
    /// <param name="tags">Etiquetas por las que se podrá invalidar esta entrada.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El valor, sea de la caché o calculado.</returns>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, IEnumerable<string>? tags = null, CancellationToken ct = default);

    /// <summary>
    /// Elimina una entrada y todo su índice (recencia y etiquetas).
    /// </summary>
    /// <param name="key">Clave.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Invalidación selectiva: elimina solo las entradas marcadas con la etiqueta.
    /// Es la invalidación que se usa cuando cambian reservas o estados (D4).
    /// </summary>
    /// <param name="tag">Etiqueta.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task RemoveByTagAsync(string tag, CancellationToken ct = default);

    /// <summary>
    /// Vacía por completo la caché y su índice.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    Task ClearAsync(CancellationToken ct = default);

    /// <summary>
    /// Poda el índice de recencia y etiquetas: elimina las referencias a entradas
    /// que Redis ya caducó. Invocable a mano para probarla sin esperar al timer (RF-40).
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Número de entradas podadas.</returns>
    Task<int> PruneAsync(CancellationToken ct = default);
}
