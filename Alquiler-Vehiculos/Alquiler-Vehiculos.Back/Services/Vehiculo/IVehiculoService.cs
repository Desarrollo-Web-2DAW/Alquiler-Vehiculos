using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Query;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Services.Common;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Vehiculo;

public interface IVehiculoService : ICrudService<CreateVehiculoDto, UpdateVehiculoDto, VehiculoDto, string>
{
    /// <summary>
    /// Obtiene un vehículo por su matrícula.
    /// </summary>
    /// <param name="matricula">Matrícula del vehículo que se quiere consultar.</param>
    /// <returns>El vehículo encontrado o el correspondiente error.</returns>
    Task<Result<VehiculoDto, DomainError>> GetByMatricula(string matricula);

    /// <summary>
    /// Busca vehículos por filtros y devuelve los recuentos agrupados por marca, modelo y categoría.
    /// </summary>
    /// <param name="request">Filtros opcionales del catálogo y rango de fechas solicitado.</param>
    /// <param name="esAdmin">
    /// Indica si se permite ver grupos sin unidades libres. El controlador lo obtiene del usuario
    /// autenticado, nunca de los datos enviados por el cliente. Por defecto se aplica la vista pública.
    /// </param>
    /// <returns>Los grupos con sus unidades totales y libres, o un error de dominio si la petición no es válida.</returns>
    /// <remarks>
    /// Actualmente cuenta la disponibilidad según el estado del vehículo.
    /// Las búsquedas con fechas devuelven un error explícito mientras no esté integrado
    /// el contrato de disponibilidad del módulo de reservas; no se calcula un recuento ficticio.
    /// </remarks>
    Task<Result<IEnumerable<QueryResponseVehiculoDto>, DomainError>> GetByFilters(QueryRequestVehiculoDto request, bool esAdmin = false);

    /// <summary>
    /// Obtiene una página de vehículos y el total de registros disponibles.
    /// </summary>
    /// <param name="page">Número de página (1-based).</param>
    /// <param name="pageSize">Cantidad de vehículos por página.</param>
    /// <returns>Los vehículos de la página y el total de registros.</returns>
    Task<(IEnumerable<VehiculoDto> Items, int TotalItems)> GetPaged(int page, int pageSize);
    
    /// <summary>
    /// Cambia la propiedad Activo de un vehiculo de false a true.
    /// </summary>
    /// <param name="key">El Id del Vehículo</param>
    /// <returns>El vehículo restaurado o el correspondiente error</returns>
    Task<Result<VehiculoDto, DomainError>> Restore(string key);
}
