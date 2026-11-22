using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Repositories.Common;

namespace Alquiler_Vehiculos.Repositories.Vehiculo.Concesionario;

public interface IConcesionarioRepository : ICrud<ConcesionarioModel, long>
{
    /// <summary>
    /// Comprueba si un concesionario existe en base a su Id
    /// </summary>
    /// <param name="key">El id</param>
    /// <returns>True si existe, false si no existe</returns>
    Task<bool> ExistById(long key);

    /// <summary>
    /// Comprueba si un concesionario existe en base a su Telefono
    /// </summary>
    /// <param name="telefono">El telefono</param>
    /// <returns>True si existe, false si no existe</returns>
    Task<bool> ExistsByTelefono(string telefono);
    
    /// <summary>
    /// Obtiene los vehiculos relacionados a un concesionario
    /// </summary>
    /// <param name="key">El id</param>
    /// <returns>Los vehiculos relacionados</returns>
    Task<IEnumerable<VehiculoModel>> GetVehiculos(long key);
    
    /// <summary>
    /// Comprueba si un concesionario tiene algun vehiculo relacionado
    /// </summary>
    /// <param name="key">El id</param>
    /// <returns>True si tiene algun vehiculo relacionado, false en caso contrario</returns>
    Task<bool> HasVehiculos(long key);
}