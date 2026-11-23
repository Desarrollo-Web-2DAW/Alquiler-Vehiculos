using Alquiler_Vehiculos.Errors.Common;

namespace Alquiler_Vehiculos.Errors.Oficina;

/// <summary>
/// Errores de dominio del módulo de Oficina (RF-11, CU-18).
/// </summary>
public static class OficinaError {
    /// <summary>La oficina única del sistema no está configurada (HTTP 404).</summary>
    public static NotFoundError NoConfigurada() =>
        new("OFICINA_NO_ENCONTRADA", "La oficina única del sistema no está configurada");

    /// <summary>Trabajador candidato a responsable no encontrado (HTTP 404).</summary>
    public static NotFoundError ResponsableNoEncontrado(Guid trabajadorId) =>
        NotFoundError.FromId(trabajadorId, "Trabajador", "TRABAJADOR_NO_ENCONTRADO");

    /// <summary>El trabajador está inactivo o dado de baja lógica (HTTP 422).</summary>
    public static BusinessRuleError ResponsableInactivo(Guid trabajadorId) =>
        new("TRABAJADOR_INACTIVO",
            $"El trabajador con ID '{trabajadorId}' está inactivo o dado de baja y no puede ser responsable");

    /// <summary>El responsable de oficina debe pertenecer a Operaciones (HTTP 422).</summary>
    public static BusinessRuleError ResponsableDebeSerOperaciones(Guid trabajadorId) =>
        new("RESPONSABLE_DEBE_SER_OPERACIONES",
            $"El trabajador con ID '{trabajadorId}' debe ser de tipo Operaciones para dirigir la oficina");

    /// <summary>Error de validación con detalle por campo (HTTP 400).</summary>
    public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) =>
        ValidationError.WithFieldErrors(errores);
}
