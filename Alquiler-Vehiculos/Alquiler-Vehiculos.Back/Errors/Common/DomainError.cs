namespace Alquiler_Vehiculos.Errors.Common;

/// <summary>
/// Clase base para errores del dominio.
/// Cada error lleva un código estable (para el cliente) y un mensaje (para la persona).
/// </summary>
public abstract record DomainError(string Code, string Message) {
    /// <summary>Representación formateada del error.</summary>
    public override string ToString() {
        return $"{Code}: {Message}";
    }
}

/// <summary>
/// Recurso no encontrado (HTTP 404).
/// </summary>
public sealed record NotFoundError(string Code, string Message) : DomainError(Code, Message) {
    /// <summary>Crea el error para una entidad identificada por su GUID.</summary>
    /// <param name="id">ID del recurso que no existe.</param>
    /// <param name="resourceType">Tipo de recurso para el mensaje.</param>
    /// <param name="code">Código estable que recibe el cliente.</param>
    /// <returns>NotFoundError formateado.</returns>
    public static NotFoundError FromId(Guid id, string resourceType, string code) {
        return new NotFoundError(code, $"{resourceType} con ID '{id}' no encontrado");
    }

    /// <summary>Crea el error para una entidad identificada por su nombre.</summary>
    /// <param name="name">Nombre buscado.</param>
    /// <param name="resourceType">Tipo de recurso para el mensaje.</param>
    /// <param name="code">Código estable que recibe el cliente.</param>
    /// <returns>NotFoundError formateado.</returns>
    public static NotFoundError FromName(string name, string resourceType, string code) {
        return new NotFoundError(code, $"{resourceType} con nombre '{name}' no encontrado");
    }

    /// <summary>Crea el error para una entidad identificada por su email.</summary>
    /// <param name="email">Email buscado.</param>
    /// <param name="resourceType">Tipo de recurso para el mensaje.</param>
    /// <param name="code">Código estable que recibe el cliente.</param>
    /// <returns>NotFoundError formateado.</returns>
    public static NotFoundError FromEmail(string email, string resourceType, string code) {
        return new NotFoundError(code, $"{resourceType} con email '{email}' no encontrado");
    }
}

/// <summary>
/// Error de validación de datos (HTTP 400).
/// </summary>
public sealed record ValidationError(string Message, Dictionary<string, string[]> ValidationErrors)
    : DomainError("VALIDACION", Message) {
    /// <summary>Crea error de validación con errores por campo.</summary>
    /// <param name="fieldErrors">Diccionario de errores por campo.</param>
    /// <returns>ValidationError lista para retornar.</returns>
    public static ValidationError WithFieldErrors(Dictionary<string, string[]> fieldErrors) {
        return new ValidationError("Errores de validación", fieldErrors);
    }

    /// <summary>Crea error de validación simple, sin detalle por campo.</summary>
    /// <param name="mensaje">Mensaje general del fallo.</param>
    /// <returns>ValidationError con diccionario vacío.</returns>
    public static ValidationError Create(string mensaje) {
        return new ValidationError(mensaje, new Dictionary<string, string[]>());
    }
}

/// <summary>
/// Violación de regla de negocio (HTTP 422).
/// </summary>
public sealed record BusinessRuleError(string Code, string Message) : DomainError(Code, Message) {
}

/// <summary>
/// No autenticado (HTTP 401).
/// </summary>
public sealed record UnauthorizedError(string Code, string Message) : DomainError(Code, Message) {
}

/// <summary>
/// Sin permisos (HTTP 403).
/// </summary>
public sealed record ForbiddenError(string Code, string Message) : DomainError(Code, Message) {
}

/// <summary>
/// Conflicto con el estado actual del recurso (HTTP 409).
/// </summary>
public sealed record ConflictError(string Code, string Message) : DomainError(Code, Message) {
}

/// <summary>
/// Error interno del servidor (HTTP 500).
/// </summary>
public sealed record InternalError(string Message) : DomainError("ERROR_INTERNO", Message) {
}
