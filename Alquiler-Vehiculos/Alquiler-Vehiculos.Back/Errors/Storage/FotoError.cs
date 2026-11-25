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

    public static ConflictError FotoReferenciada(string nombre)
    {
        return new ConflictError("FOTO_REFERENCIADA", $"No se puede dar de baja ni eliminar la foto '{nombre}' porque está asociada a un vehículo.");
    }

    /// <summary>
    /// Genera un error cuando no se puede dar de baja una foto
    /// </summary>
    /// <param name="nombre">Nombre de la foto que no se pudo dar de baja.</param>
    /// <returns>Error técnico de tipo <see cref="InternalError"/> (HTTP 500).</returns>
    public static InternalError NoSePudoDarDeBaja(string nombre)
    {
        return new InternalError(
            $"No se pudo actualizar el vehículo: fallo al dar de baja la foto anterior '{nombre}'.");
    }

    /// <summary>
    /// Genera un error cuando no se puede restaurar una foto
    /// </summary>
    /// <param name="nombre">Nombre de la foto que no se pudo restaurar.</param>
    /// <returns>Error técnico de tipo <see cref="InternalError"/> (HTTP 500).</returns>
    public static InternalError NoSePudoRestaurar(string nombre)
    {
        return new InternalError(
            $"No se pudo restaurar la foto '{nombre}' asociada al vehículo.");
    }
    
    
    /// <summary>
    /// Genera un error cuando no se puede crear una foto
    /// </summary>
    /// <param name="nombre">Nombre de la foto que no se pudo restaurar.</param>
    /// <returns>Error técnico de tipo <see cref="BusinessRuleError"/> (HTTP 422).</returns>
    public static InternalError NosePudoCrear(string nombre)
    {
        return new InternalError($"No se pudo restaurar la foto '{nombre}' asociada al vehículo.");
    }
}
