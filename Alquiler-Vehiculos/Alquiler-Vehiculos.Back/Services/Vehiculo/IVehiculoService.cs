using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;

using Alquiler_Vehiculos.Services.Common;

namespace Alquiler_Vehiculos.Services.Vehiculo;

public interface IVehiculoService : ICrudService<CreateVehiculoDto, UpdateVehiculoDto, VehiculoDto, string>
{
    /// <summary>
    /// Obtiene una página de vehículos y el total de registros disponibles.
    /// </summary>
    /// <param name="page">Número de página (1-based).</param>
    /// <param name="pageSize">Cantidad de vehículos por página.</param>
    /// <returns>Los vehículos de la página y el total de registros.</returns>
    Task<(IEnumerable<VehiculoDto> Items, int TotalItems)> GetPaged(int page, int pageSize);
}