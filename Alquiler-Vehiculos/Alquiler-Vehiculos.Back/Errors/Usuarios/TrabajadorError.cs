using Alquiler_Vehiculos.Errors.Common;

namespace Alquiler_Vehiculos.Errors.Usuarios;

/// <summary>
/// Errores del dominio de trabajadores (HTTP 404, 409, 422).
/// Son errores de la persona; los de su cuenta viven en <c>Errors/Auth</c>.
/// </summary>
public static class TrabajadorError {
    /// <summary>Trabajador no encontrado por su identificador (HTTP 404).</summary>
    /// <param name="id">ID del trabajador.</param>
    /// <returns>NotFoundError con código TRABAJADOR_NO_ENCONTRADO.</returns>
    public static NotFoundError NotFound(Guid id) {
        return NotFoundError.FromId(id, "Trabajador", "TRABAJADOR_NO_ENCONTRADO");
    }

    /// <summary>Trabajador no encontrado por su correo electrónico (HTTP 404).</summary>
    /// <param name="email">Correo buscado.</param>
    /// <returns>NotFoundError con código TRABAJADOR_NO_ENCONTRADO.</returns>
    public static NotFoundError NotFoundByEmail(string email) {
        return NotFoundError.FromEmail(email, "Trabajador", "TRABAJADOR_NO_ENCONTRADO");
    }

    /// <summary>Ya existe un trabajador con ese email (HTTP 409).</summary>
    /// <param name="email">Email duplicado.</param>
    /// <returns>ConflictError con código EMAIL_DUPLICADO.</returns>
    public static ConflictError EmailExistente(string email) {
        return new ConflictError("EMAIL_DUPLICADO", $"Ya existe un trabajador con el email '{email}'");
    }

    /// <summary>Ya existe un trabajador con ese DNI (HTTP 409).</summary>
    /// <param name="dni">DNI duplicado.</param>
    /// <returns>ConflictError con código DOCUMENTO_DUPLICADO.</returns>
    public static ConflictError DniExistente(string dni) {
        return new ConflictError("DOCUMENTO_DUPLICADO", $"Ya existe un trabajador con el DNI '{dni}'");
    }

    /// <summary>No se puede dar de baja al responsable de la oficina (CU-18, HTTP 409).</summary>
    /// <param name="id">ID del trabajador.</param>
    /// <returns>ConflictError con código TRABAJADOR_ES_RESPONSABLE.</returns>
    public static ConflictError EsResponsableOficina(Guid id) {
        return new ConflictError(
            "TRABAJADOR_ES_RESPONSABLE",
            $"El trabajador con ID '{id}' es el responsable actual de la oficina y no puede darse de baja hasta nombrar a otro");
    }

    /// <summary>No se puede dar de baja a un trabajador con reservas vivas creadas por él (RF-12, HTTP 409).</summary>
    /// <param name="id">ID del trabajador.</param>
    /// <returns>ConflictError con código TRABAJADOR_CON_RESERVAS_VIVAS.</returns>
    public static ConflictError ConReservasVivas(Guid id) {
        return new ConflictError(
            "TRABAJADOR_CON_RESERVAS_VIVAS",
            $"El trabajador con ID '{id}' tiene reservas vivas asociadas y no puede darse de baja");
    }

    /// <summary>El trabajador está inactivo y no puede operar (HTTP 422).</summary>
    /// <param name="id">ID del trabajador.</param>
    /// <returns>BusinessRuleError con código TRABAJADOR_INACTIVO.</returns>
    public static BusinessRuleError TrabajadorInactivo(Guid id) {
        return new BusinessRuleError(
            "TRABAJADOR_INACTIVO",
            $"El trabajador con ID '{id}' está inactivo y no puede operar");
    }
}
