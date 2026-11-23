using Alquiler_Vehiculos.Errors.Common;

namespace Alquiler_Vehiculos.Errors.Taller;

/// <summary>
/// Errores del dominio de los partes de taller (HTTP 400, 403, 404, 409).
/// Son documentos internos del taller (RF-08): los errores de la persona que
/// los tramita viven en <c>Errors/Usuarios</c>.
/// </summary>
public static class ParteTallerError {
    /// <summary>Parte de taller no encontrado por su identificador (HTTP 404).</summary>
    /// <param name="id">ID del parte.</param>
    /// <returns>NotFoundError con código PARTE_NO_ENCONTRADA.</returns>
    public static NotFoundError NotFound(Guid id) {
        return NotFoundError.FromId(id, "Parte de taller", "PARTE_NO_ENCONTRADA");
    }

    /// <summary>
    /// El vehículo ya tiene un parte abierto: no puede entrar al taller dos veces
    /// sin cerrar el anterior (RF-08, HTTP 409).
    /// </summary>
    /// <param name="vehiculoId">Identificador del vehículo.</param>
    /// <returns>ConflictError con código PARTE_ABIERTA_EXISTENTE.</returns>
    public static ConflictError ParteAbiertaExistente(string vehiculoId) {
        return new ConflictError(
            "PARTE_ABIERTA_EXISTENTE",
            $"El vehículo '{vehiculoId}' ya tiene un parte de taller abierto; ciérralo antes de abrir otro (RF-08)");
    }

    /// <summary>
    /// El parte ya está cerrado: la única transición válida es de Abierto a
    /// Cerrado y no se reabren partes (RF-08, RF-32, HTTP 409).
    /// </summary>
    /// <param name="id">ID del parte.</param>
    /// <returns>ConflictError con código PARTE_YA_CERRADA.</returns>
    public static ConflictError YaCerrada(Guid id) {
        return new ConflictError(
            "PARTE_YA_CERRADA",
            $"El parte de taller con ID '{id}' ya está cerrado y no se puede volver a cerrar ni reabrir (RF-08)");
    }

    /// <summary>
    /// Solo un trabajador de tipo <c>Mecanico</c> abre y cierra partes (RF-08, RF-12, HTTP 403).
    /// </summary>
    /// <param name="trabajadorId">ID del trabajador que lo intenta.</param>
    /// <returns>ForbiddenError con código SOLO_MECANICO.</returns>
    public static ForbiddenError SoloMecanico(Guid trabajadorId) {
        return new ForbiddenError(
            "SOLO_MECANICO",
            $"Solo un trabajador de tipo Mecánico puede abrir y cerrar partes de taller (RF-08); el trabajador '{trabajadorId}' no lo es");
    }

    /// <summary>Error de validación de datos con detalle por campo (HTTP 400).</summary>
    /// <param name="errores">Diccionario de errores por campo.</param>
    /// <returns>ValidationError.</returns>
    public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) {
        return ValidationError.WithFieldErrors(errores);
    }
}
