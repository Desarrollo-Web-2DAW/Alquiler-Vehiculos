namespace Alquiler_Vehiculos.Dto.Common;

/// <summary>
/// Respuesta paginada tipada. Existe como tipo concreto, y no como objeto anónimo,
/// para que el controlador pueda firmar con <c>ActionResult&lt;PagedResponse&lt;T&gt;&gt;</c>
/// y la documentación de la API muestre la forma real.
/// </summary>
/// <remarks>
/// El JSON es: <c>{"data":[...],"pagination":{"page":1,"pageSize":10,"totalPages":1,"totalItems":0}}</c>.
/// </remarks>
/// <typeparam name="T">Tipo de elemento de la página actual.</typeparam>
public sealed record PagedResponse<T> {
    /// <summary>Elementos de la página actual.</summary>
    public IEnumerable<T> Data { get; init; } = Enumerable.Empty<T>();

    /// <summary>Metadatos de la paginación.</summary>
    public PageMetadata Pagination { get; init; } = new(0, 0, 0, 0);
}

/// <summary>
/// Metadatos de la paginación de una respuesta <see cref="PagedResponse{T}"/>.
/// </summary>
/// <param name="Page">Página actual, empezando en 1.</param>
/// <param name="PageSize">Tamaño de página solicitado.</param>
/// <param name="TotalPages">Número total de páginas.</param>
/// <param name="TotalItems">Número total de elementos.</param>
public sealed record PageMetadata(int Page, int PageSize, int TotalPages, int TotalItems);