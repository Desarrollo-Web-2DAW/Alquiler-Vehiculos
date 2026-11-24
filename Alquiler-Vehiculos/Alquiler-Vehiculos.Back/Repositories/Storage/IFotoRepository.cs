using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Repositories.Common;

namespace Alquiler_Vehiculos.Repositories.Storage;

public interface IFotoRepository : ICrud<Foto, string>
{
    /// <summary>
    /// Comprueba si una foto existe en base a su Id
    /// </summary>
    /// <param name="name">El id</param>
    /// <returns>True si existe, false si no existe</returns>
    Task<bool> ExistById(string name);
    
    /// <summary>
    /// Restaura una foto por su Id
    /// </summary>
    /// <param name="name">El id</param>
    /// <returns><see cref="Foto"/>si existe, null si no existe</returns>
    Task<Foto?> Restore(string name);
    
    /// <summary>
    /// Borra permanentemente una foto
    /// </summary>
    /// <param name="name">El id</param>
    /// <returns>True si se ha borrado, false si no se ha borrado</returns>
    Task<bool> HardDelete(string name);
}