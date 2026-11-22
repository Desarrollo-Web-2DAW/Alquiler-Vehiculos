using Alquiler_Vehiculos.Errors.Common;

namespace Alquiler_Vehiculos.Errors.Vehiculo;

/// <summary>Errores de dominio del catálogo de marcas.</summary>
public static class MarcaError {
    /// <summary>La marca no existe o está dada de baja.</summary>
    public static NotFoundError NotFoundMarca(string id) =>
        new("MARCA_NO_ENCONTRADA", $"Marca con ID '{id}' no encontrada.");

    /// <summary>No se pudo realizar la baja lógica.</summary>
    public static BusinessRuleError BorradoError() =>
        new("BORRADO_MARCA", "No se ha podido completar la baja lógica de la marca.");
}
