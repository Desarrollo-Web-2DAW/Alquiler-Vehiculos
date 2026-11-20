using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Repositories.Common;

namespace Alquiler_Vehiculos.Repositories.Vehiculo.Concesionario;

public interface IConcesionarioRepository : ICrud<ConcesionarioModel, long>
{
    /// <summary>
    /// Comprueba si un vehiculo existe en base a su Id
    /// </summary>
    /// <param name="key">El id</param>
    /// <returns>True si existe, false si no existe</returns>
    Task<bool> ExistById(long key);
}