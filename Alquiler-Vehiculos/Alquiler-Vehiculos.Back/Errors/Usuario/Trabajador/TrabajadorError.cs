using Alquiler_Vehiculos.Errors.Common;

namespace Alquiler_Vehiculos.Errors.Usuario.Trabajador;

public class TrabajadorError {
    public static NotFoundError TrabajadorNotFound(Guid id) {
        return NotFoundError.FromId(id, "Trabajador");
    }
    
    public static NotFoundError TrabajadorNotFoundByEmail(string email) {
        return NotFoundError.FromEmail(email, "Trabajador");
    }
    
    public static ConflictError TrabajadorEmailAlreadyExists(string email) {
        return new ConflictError($"Ya existe un trabajador con el email '{email}'");
    }
    
    public static ConflictError TrabajadorDniAlreadyExists(string dni) {
        return new ConflictError($"Ya existe un trabajador con el DNI '{dni}'");
    }
    
    /// <summary>RF-12 / CU-18: No se puede dar de baja al responsable de la oficina.</summary>
    public static ConflictError TrabajadorEsResponsableOficina(Guid id) {
        return new ConflictError($"El trabajador con ID '{id}' es el responsable actual de la oficina y no puede ser dado de baja hasta nombrar a otro.");
    }
    
    /// <summary>RF-12 / CU-18: No se puede dar de baja a un trabajador con reservas vivas creadas por él.</summary>
    public static ConflictError TrabajadorConReservasVivas(Guid id) {
        return new ConflictError($"El trabajador con ID '{id}' tiene reservas vivas asociadas y no puede ser dado de baja.");
    }
    
    public static BadRequestError TrabajadorBadRequestError(string name) {
        return new BadRequestError($"Reserva no encontrada para el trabajador '{name}'");
    }
}