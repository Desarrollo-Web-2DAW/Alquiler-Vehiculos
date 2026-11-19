using Alquiler_Vehiculos.Errors.Common;

namespace Alquiler_Vehiculos.Errors.Usuarios;

/// <summary>
/// Errores del dominio de clientes (HTTP 400, 404, 409, 422).
/// Son errores de la persona; los de su cuenta viven en <c>Errors/Auth</c>.
/// </summary>
public static class ClienteError {
    /// <summary>Cliente no encontrado por su identificador (HTTP 404).</summary>
    /// <param name="id">ID del cliente.</param>
    /// <returns>NotFoundError con código CLIENTE_NO_ENCONTRADO.</returns>
    public static NotFoundError NotFound(Guid id) {
        return NotFoundError.FromId(id, "Cliente", "CLIENTE_NO_ENCONTRADO");
    }

    /// <summary>Cliente no encontrado por su correo electrónico (HTTP 404).</summary>
    /// <param name="email">Correo buscado.</param>
    /// <returns>NotFoundError con código CLIENTE_NO_ENCONTRADO.</returns>
    public static NotFoundError NotFoundByEmail(string email) {
        return NotFoundError.FromEmail(email, "Cliente", "CLIENTE_NO_ENCONTRADO");
    }

    /// <summary>Ya existe un cliente con ese correo electrónico (HTTP 409).</summary>
    /// <param name="email">Correo duplicado.</param>
    /// <returns>ConflictError con código EMAIL_DUPLICADO.</returns>
    public static ConflictError EmailExistente(string email) {
        return new ConflictError("EMAIL_DUPLICADO", $"Ya existe un cliente con el email '{email}'");
    }

    /// <summary>Ya existe un cliente con ese teléfono (HTTP 409).</summary>
    /// <param name="telefono">Teléfono duplicado.</param>
    /// <returns>ConflictError con código TELEFONO_DUPLICADO.</returns>
    public static ConflictError TelefonoExistente(string telefono) {
        return new ConflictError("TELEFONO_DUPLICADO", $"Ya existe un cliente con el teléfono '{telefono}'");
    }

    /// <summary>Ya existe un cliente con ese documento (DNI, NIE o pasaporte) (HTTP 409).</summary>
    /// <param name="documento">Número del documento duplicado.</param>
    /// <returns>ConflictError con código DOCUMENTO_DUPLICADO.</returns>
    public static ConflictError DocumentoExistente(string documento) {
        return new ConflictError("DOCUMENTO_DUPLICADO", $"Ya existe un cliente con el documento '{documento}'");
    }

    /// <summary>No se puede dar de baja un cliente con reservas vivas (CU-19, HTTP 409).</summary>
    /// <param name="id">ID del cliente.</param>
    /// <returns>ConflictError con código CLIENTE_CON_RESERVAS_VIVAS.</returns>
    public static ConflictError ConReservasVivas(Guid id) {
        return new ConflictError(
            "CLIENTE_CON_RESERVAS_VIVAS",
            $"El cliente con ID '{id}' tiene reservas vivas y no puede darse de baja");
    }

    /// <summary>El cliente está inactivo y no puede operar (HTTP 422).</summary>
    /// <param name="id">ID del cliente.</param>
    /// <returns>BusinessRuleError con código CLIENTE_INACTIVO.</returns>
    public static BusinessRuleError ClienteInactivo(Guid id) {
        return new BusinessRuleError(
            "CLIENTE_INACTIVO",
            $"El cliente con ID '{id}' está inactivo y no puede realizar reservas");
    }

    /// <summary>El cliente es menor de edad (RF-13, HTTP 422).</summary>
    /// <param name="edad">Edad cumplida en la fecha de la operación.</param>
    /// <returns>BusinessRuleError con código CLIENTE_MENOR_DE_EDAD.</returns>
    public static BusinessRuleError ClienteMenorDeEdad(int edad) {
        return new BusinessRuleError(
            "CLIENTE_MENOR_DE_EDAD",
            $"El cliente tiene {edad} años. Debe ser mayor de edad para alquilar.");
    }

    /// <summary>La fecha de nacimiento es incorrecta (HTTP 400).</summary>
    /// <returns>ValidationError.</returns>
    public static ValidationError FechaNacimientoInvalida() {
        return ValidationError.Create("La fecha de nacimiento no puede ser una fecha futura.");
    }
}
