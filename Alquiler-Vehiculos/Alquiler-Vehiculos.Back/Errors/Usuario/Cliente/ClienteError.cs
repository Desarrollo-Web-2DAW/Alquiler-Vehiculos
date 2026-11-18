using Alquiler_Vehiculos.Errors.Common;

namespace Alquiler_Vehiculos.Errors.Usuario.Cliente;


/// <summary>
/// Errores del dominio de clientes (HTTP 404, 409, 400, 422).
/// </summary>
public static class ClienteError {
    /// <summary>Cliente no encontrado por su identificador.</summary>
    /// <param name="id">ID del cliente.</param>
    /// <returns>NotFoundError (HTTP 404).</returns>
    public static NotFoundError ClienteNotFound(Guid id) {
        return NotFoundError.FromId(id, "Cliente");
    }
    
    /// <summary>Cliente no encontrado por su correo electrónico.</summary>
    /// <param name="email">Correo buscado.</param>
    /// <returns>NotFoundError (HTTP 404).</returns>
    public static NotFoundError ClienteNotFoundByEmail(string email) {
        return NotFoundError.FromEmail(email, "Cliente");
    }
    
    /// <summary>Ya existe un cliente con ese correo electrónico.</summary>
    /// <param name="email">Correo duplicado.</param>
    /// <returns>ConflictError (HTTP 409).</returns>
    public static ConflictError EmailAlreadyExists(string email) {
        return new ConflictError($"Ya existe un cliente con el email '{email}'");
    }
    
    /// <summary>Ya existe un cliente con ese documento (DNI, NIE o pasaporte).</summary>
    /// <param name="tipo">Tipo de documento: DNI, NIE o Pasaporte.</param>
    /// <param name="valor">Número del documento duplicado.</param>
    /// <returns>ConflictError (HTTP 409).</returns>
    public static ConflictError DocumentoAlreadyExists(string documento) {
        return new ConflictError($"Ya existe un cliente con el documento '{documento}'");
    }
    
    /// <summary>No se puede dar de baja un cliente con reservas vivas (RF-13).</summary>
    /// <param name="id">ID del cliente.</param>
    /// <returns>ConflictError (HTTP 409).</returns>
    public static ConflictError ClienteConReservaActiva(Guid id) {
        return new ConflictError($"El cliente con ID '{id}' tiene una reserva activa y no puede ser eliminado");
    }
    
    
    /// <summary>El cliente está inactivo y no puede operar.</summary>
    /// <param name="id">ID del cliente.</param>
    /// <returns>BusinessRuleError (HTTP 422).</returns>
    public static BusinessRuleError InactivoCliente(Guid id) {
        return new BusinessRuleError($"El cliente con ID '{id}' está inactivo y no puede realizar reservas");
    }
    
    
    public static BadRequestError ClienteBadRequestError(string name) {
        return new BadRequestError($"Reserva no encontrada para el cliente '{name}'");
    }
    
}