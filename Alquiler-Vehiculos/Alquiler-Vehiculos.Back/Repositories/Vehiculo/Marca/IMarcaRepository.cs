using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Repositories.Common;

namespace Alquiler_Vehiculos.Repositories.Vehiculo.Marca;

/// <summary>
/// Define las operaciones de persistencia de marcas del catálogo.
/// Hereda el alta, consulta, modificación y baja lógica de la interfaz CRUD.
/// </summary>
public interface IMarcaRepository : ICrud<MarcaModel, string> {
    /// <summary>Comprueba si existe una marca activa con el identificador indicado.</summary>
    /// <param name="key">ObjectId de la marca representado como cadena hexadecimal.</param>
    /// <returns>True si existe y no está dada de baja; false en caso contrario.</returns>
    Task<bool> ExistById(string key);
}
