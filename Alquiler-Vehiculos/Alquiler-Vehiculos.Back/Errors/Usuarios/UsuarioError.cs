using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Errors.Common;

namespace Alquiler_Vehiculos.Errors.Usuarios;

/// <summary>
/// Errores de administración de cuentas (HTTP 400, 404, 409).
/// </summary>
public static class UsuarioError {
    /// <summary>Cuenta no encontrada por su identificador (HTTP 404).</summary>
    /// <param name="id">ID de la cuenta.</param>
    /// <returns>NotFoundError con código USUARIO_NO_ENCONTRADO.</returns>
    public static NotFoundError NotFound(Guid id) {
        return NotFoundError.FromId(id, "Usuario", "USUARIO_NO_ENCONTRADO");
    }

    /// <summary>No se puede dejar el sistema sin ningún administrador activo (HTTP 409).</summary>
    /// <returns>ConflictError con código ULTIMO_ADMIN.</returns>
    public static ConflictError UltimoAdmin() {
        return new ConflictError(
            "ULTIMO_ADMIN",
            "No se puede dejar el sistema sin ningún administrador activo");
    }

    /// <summary>El rol indicado no existe en el sistema (HTTP 400).</summary>
    /// <param name="rol">Nombre del rol recibido.</param>
    /// <returns>ValidationError con los valores permitidos.</returns>
    public static ValidationError RolInvalido(string rol) {
        var permitidos = string.Join(", ", Enum.GetNames<RolUsuario>());
        return ValidationError.Create($"El rol '{rol}' no es válido. Valores permitidos: {permitidos}");
    }

    /// <summary>
    /// El rol no encaja con la persona de la cuenta (HTTP 409).
    /// Un cliente solo puede ser Cliente; un trabajador puede ser Trabajador,
    /// Responsable o Admin.
    /// </summary>
    /// <param name="rol">Rol solicitado.</param>
    /// <param name="id">ID de la cuenta.</param>
    /// <returns>ConflictError con código ROL_INCOMPATIBLE_CON_PERSONA.</returns>
    public static ConflictError RolIncompatibleConPersona(string rol, Guid id) {
        return new ConflictError(
            "ROL_INCOMPATIBLE_CON_PERSONA",
            $"El rol '{rol}' no es compatible con la persona de la cuenta '{id}'. " +
            "Un cliente solo puede ser Cliente; un trabajador puede ser Trabajador, Responsable o Admin.");
    }

    /// <summary>Error de validación con detalle por campo (HTTP 400).</summary>
    /// <param name="errores">Diccionario de errores por campo.</param>
    /// <returns>ValidationError.</returns>
    public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) {
        return ValidationError.WithFieldErrors(errores);
    }
}
