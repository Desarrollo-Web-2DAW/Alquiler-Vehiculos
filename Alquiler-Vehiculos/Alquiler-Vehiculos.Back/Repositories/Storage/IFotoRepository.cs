using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Repositories.Common;

namespace Alquiler_Vehiculos.Repositories.Storage;

public interface IFotoRepository : ICrud<Foto, string>
{
    /// <summary>
    /// Comprueba si un vehiculo existe en base a su Id
    /// </summary>
    /// <param name="name">El id</param>
    /// <returns>True si existe, false si no existe</returns>
    Task<bool> ExistById(string name);
}