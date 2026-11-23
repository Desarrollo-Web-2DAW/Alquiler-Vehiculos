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
        return new NotFoundError("FOTO_NO_ENCONTRADA", $"No se ha encontrado la foto con el nombre:{nombre}");
    }

    /// <summary>
    /// Genera un error cuando no se puede dar de baja la foto anterior de un vehículo
    /// durante la sustitución de su imagen.
    /// </summary>
    /// <param name="nombre">Nombre de la foto que no se pudo dar de baja.</param>
    /// <returns>Error técnico de tipo <see cref="InternalError"/> (HTTP 500).</returns>
    public static InternalError NoSePudoDarDeBaja(string nombre)
    {
        return new InternalError(
            $"No se pudo actualizar el vehículo: fallo al dar de baja la foto anterior '{nombre}'.");
    }

    /// <summary>
    /// Genera un error cuando no se puede restaurar la foto asociada a un vehículo
    /// al reactivar el vehículo.
    /// </summary>
    /// <param name="nombre">Nombre de la foto que no se pudo restaurar.</param>
    /// <returns>Error técnico de tipo <see cref="InternalError"/> (HTTP 500).</returns>
    public static InternalError NoSePudoRestaurar(string nombre)
    {
        return new InternalError(
            $"No se pudo restaurar la foto '{nombre}' asociada al vehículo.");
    }
}
