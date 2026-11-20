using Alquiler_Vehiculos.Errors.Common;

namespace Alquiler_Vehiculos.Errors.Storage;

/// <summary>
/// Clase que proporciona los errores relacionados con el almacenamiento de archivos.
/// </summary>
public static class StorageError {
    /// <summary>
    /// Genera un error cuando no se recibe un archivo o su contenido está vacío.
    /// </summary>
    /// <returns>Error de tipo BusinessRuleError.</returns>
    public static BusinessRuleError ArchivoVacio() {
        return new BusinessRuleError("ARCHIVO_VACIO", "El archivo está vacío o no se ha proporcionado.");
    }

    /// <summary>
    /// Genera un error cuando el archivo supera el tamaño máximo permitido.
    /// </summary>
    /// <returns>Error de tipo BusinessRuleError.</returns>
    public static BusinessRuleError ArchivoMuyGrande() {
        return new BusinessRuleError("ARCHIVO_MUY_GRANDE", "El archivo supera el tamaño máximo permitido.");
    }

    /// <summary>
    /// Genera un error cuando la extensión del archivo no está permitida.
    /// </summary>
    /// <returns>Error de tipo BusinessRuleError.</returns>
    public static BusinessRuleError ExtensionNoPermitida() {
        return new BusinessRuleError("EXTENSION_NO_PERMITIDA", "La extensión del archivo no está permitida.");
    }

    /// <summary>
    /// Genera un error cuando el tipo de contenido del archivo no está permitido.
    /// </summary>
    /// <returns>Error de tipo BusinessRuleError.</returns>
    public static BusinessRuleError TipoContenidoNoPermitido() {
        return new BusinessRuleError("TIPO_CONTENIDO_NO_PERMITIDO", "El tipo de contenido del archivo no está permitido.");
    }

    /// <summary>
    /// Genera un error cuando el nombre del archivo contiene caracteres no válidos.
    /// </summary>
    /// <returns>Error de tipo BusinessRuleError.</returns>
    public static BusinessRuleError NombreArchivoInvalido() {
        return new BusinessRuleError("NOMBRE_ARCHIVO_INVALIDO", "El nombre del archivo contiene caracteres no válidos.");
    }

    /// <summary>
    /// Genera un error interno cuando falla el guardado del archivo.
    /// </summary>
    /// <returns>Error de tipo InternalError.</returns>
    public static InternalError ErrorGuardando() {
        return new InternalError("No se ha podido guardar el archivo en el servidor.");
    }

    /// <summary>
    /// Genera un error interno cuando falla la eliminación del archivo.
    /// </summary>
    /// <returns>Error de tipo InternalError.</returns>
    public static InternalError ErrorEliminando() {
        return new InternalError("No se ha podido eliminar el archivo del servidor.");
    }
}
