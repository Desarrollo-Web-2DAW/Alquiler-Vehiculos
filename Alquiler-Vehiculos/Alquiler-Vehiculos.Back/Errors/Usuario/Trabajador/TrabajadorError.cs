using Alquiler_Vehiculos.Errors.Common;

namespace Alquiler_Vehiculos.Errors.Usuario.Trabajador;

public class TrabajadorError {
    public static NotFoundError TrabajadorNotFound(Guid id) {
        return NotFoundError.FromId(id, "Trabajador");
    }
    
    public static NotFoundError TrabajadorNotFoundByName(string name) {
        return NotFoundError.FromName($"No se encontró un trabajador con el nombre '{name}'");
    }
    
    public static ConflictError TrabajadorAlreadyExists(string name) {
        return new ConflictError($"Ya existe un trabajador con el nombre '{name}'");
    }
    
    public static BadRequestError TrabajadorBadRequestError(string name) {
        return new BadRequestError($"Reserva no encontrada para el trabajador '{name}'");
    }
}