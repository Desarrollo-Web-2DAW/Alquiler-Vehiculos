using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Repositories.Common;

namespace Alquiler_Vehiculos.Repositories.Vehiculo;

public interface IVehiculoRepository : ICrud<VehiculoModel, string>
{
    /// <summary>
    /// Comprueba si un vehiculo existe en base a su Id
    /// </summary>
    /// <param name="key">El id</param>
    /// <returns>True si existe, false si no existe</returns>
    Task<bool> ExistById(string key);
    
    /// <summary>
    /// Comprueba si un vehiculo existe en base a su matricula
    /// </summary>
    /// <param name="matricula">La matricula</param>
    /// <returns>True si existe, false si no existe</returns>
    Task<bool> ExistByMatricula(string matricula);
    
    /// <summary>
    /// Obtiene un vehiculo en base a su matricula
    /// </summary>
    /// <param name="matricula">La matricula</param>
    /// <returns><see cref="VehiculoModel"/> si existe, null si no existe</returns>
    Task<VehiculoModel?> GetByMatricula(string matricula);
    
    /// <summary>
    /// Cuenta los registros y recupera únicamente la página solicitada desde la base de datos.
    /// </summary>
    /// <param name="page">Número de página, desde 1.</param>
    /// <param name="pageSize">Número máximo de elementos por página.</param>
    /// <returns>Elementos de la página y total de registros.</returns>
    Task<(IEnumerable<VehiculoModel> Items, int TotalItems)> GetPaged(int page, int pageSize);
}