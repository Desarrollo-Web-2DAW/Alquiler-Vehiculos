using Alquiler_Vehiculos.Errors.Common;

namespace Alquiler_Vehiculos.Errors.Auth;

/// <summary>
/// Errores de autenticación y alta de cuentas (HTTP 400, 401, 403, 409).
/// Son errores de la cuenta, no de la persona: los de cliente y trabajador
/// viven en <c>Errors/Usuarios</c>.
/// </summary>
public static class AuthError {
    /// <summary>Email o contraseña incorrectos (HTTP 401).</summary>
    /// <returns>UnauthorizedError con código CREDENCIALES_INVALIDAS.</returns>
    public static UnauthorizedError CredencialesInvalidas() {
        return new UnauthorizedError("CREDENCIALES_INVALIDAS", "Email o contraseña incorrectos");
    }

    /// <summary>La cuenta está suspendida o dada de baja (HTTP 403).</summary>
    /// <returns>ForbiddenError con código CUENTA_INACTIVA.</returns>
    public static ForbiddenError CuentaInactiva() {
        return new ForbiddenError("CUENTA_INACTIVA", "La cuenta está suspendida o dada de baja");
    }

    /// <summary>Demasiados intentos fallidos (HTTP 403).</summary>
    /// <returns>ForbiddenError con código CUENTA_BLOQUEADA.</returns>
    public static ForbiddenError CuentaBloqueada() {
        return new ForbiddenError("CUENTA_BLOQUEADA", "Demasiados intentos fallidos. Inténtalo más tarde");
    }

    /// <summary>La cuenta no tiene ningún rol asignado (HTTP 403).</summary>
    /// <returns>ForbiddenError con código CUENTA_SIN_ROL.</returns>
    public static ForbiddenError CuentaSinRol() {
        return new ForbiddenError("CUENTA_SIN_ROL", "La cuenta no tiene ningún rol asignado");
    }

    /// <summary>Ya existe una cuenta con ese email (HTTP 409).</summary>
    /// <param name="email">Email duplicado.</param>
    /// <returns>ConflictError con código EMAIL_DUPLICADO.</returns>
    public static ConflictError EmailExistente(string email) {
        return new ConflictError("EMAIL_DUPLICADO", $"Ya existe una cuenta con el email '{email}'");
    }

    /// <summary>Alguno de los documentos ya pertenece a otra persona (HTTP 409).</summary>
    /// <param name="documento">Documento duplicado.</param>
    /// <returns>ConflictError con código DOCUMENTO_DUPLICADO.</returns>
    public static ConflictError DocumentoExistente(string documento) {
        return new ConflictError("DOCUMENTO_DUPLICADO", $"Ya existe una persona con el documento '{documento}'");
    }

    /// <summary>Error de validación simple (HTTP 400).</summary>
    /// <param name="mensaje">Mensaje de error.</param>
    /// <returns>ValidationError.</returns>
    public static ValidationError Validacion(string mensaje) {
        return ValidationError.Create(mensaje);
    }

    /// <summary>Error de validación con detalle por campo (HTTP 400).</summary>
    /// <param name="errores">Diccionario de errores por campo.</param>
    /// <returns>ValidationError.</returns>
    public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) {
        return ValidationError.WithFieldErrors(errores);
    }
}
