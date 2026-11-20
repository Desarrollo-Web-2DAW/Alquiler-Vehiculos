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
}