using Alquiler_Vehiculos.Errors.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Alquiler_Vehiculos.Errors.Vehiculo;

public static class ConcesionarioError
{
    public static NotFoundError NotFoundConcesionario(long id)
    {
        return new NotFoundError($"No se ha encontrado el concesionario con el Id: ${id}");
    }

    public static ConflictError ConcesionarioYaExiste(string telefono)
    {
        return new ConflictError(
            $"Ya existe un concesionario con el telefono: {telefono}");
    }
    
    public static ConflictError BorradoError()
    {
        return new ConflictError(
            $"No se ha podido borrar el concesionario");
    }
}