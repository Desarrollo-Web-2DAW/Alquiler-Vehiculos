using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Storage;

namespace Alquiler_Vehiculos.Errors.Storage;

/// <summary>
/// Clase que proporciona los errores relacionados con las operaciones de la base de datos de la entidad <see cref="Foto"/>
/// </summary>
public static class FotoError
{
    /// <summary>
    /// Genera un error cuando no se encuentra una foto en la base de datos
    /// </summary>
    /// <returns>Error de tipo <see cref="NotFoundError"/>.</returns>
    public static NotFoundError FotoNotFound(string nombre)
    {
        return new NotFoundError($"No se ha encontrado la foto con el nombre:{nombre}");
    }
}