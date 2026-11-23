using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
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
