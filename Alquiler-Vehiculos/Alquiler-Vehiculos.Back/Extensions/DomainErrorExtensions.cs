using Alquiler_Vehiculos.Errors.Common;
using Microsoft.AspNetCore.Mvc;

namespace Alquiler_Vehiculos.Extensions;

/// <summary>
/// Centraliza el mapeo <see cref="DomainError"/> → HTTP en una única extensión,
/// en lugar de repetir un <c>switch</c> en cada controlador.
/// El cuerpo siempre es <c>{ code, message, details? }</c>.
/// </summary>
public static class DomainErrorExtensions {
    /// <summary>
    /// Convierte un error de dominio en su respuesta HTTP equivalente.
    /// </summary>
    /// <typeparam name="T">Tipo esperado por el endpoint.</typeparam>
    /// <param name="error">Error de dominio tipado.</param>
    /// <returns>
    /// 404 <c>NotFoundError</c> · 400 <c>ValidationError</c> (+ <c>details</c> por campo) ·
    /// 409 <c>ConflictError</c> · 422 <c>BusinessRuleError</c> · 401 <c>UnauthorizedError</c> ·
    /// 403 <c>ForbiddenError</c> · 500 <c>InternalError</c>/desconocidos.
    /// </returns>
    public static ActionResult<T> ToHttpResult<T>(this DomainError error) {
        return error switch {
            NotFoundError e => new NotFoundObjectResult(
                new { code = e.Code, message = e.Message }),
            ValidationError e => new BadRequestObjectResult(
                new { code = e.Code, message = e.Message, details = e.ValidationErrors }),
            ConflictError e => new ConflictObjectResult(
                new { code = e.Code, message = e.Message }),
            // 422: la petición está bien formada, pero la rechaza una regla de negocio.
            BusinessRuleError e => new ObjectResult(
                new { code = e.Code, message = e.Message }) { StatusCode = StatusCodes.Status422UnprocessableEntity },
            UnauthorizedError e => new UnauthorizedObjectResult(
                new { code = e.Code, message = e.Message }),
            ForbiddenError e => new ObjectResult(
                new { code = e.Code, message = e.Message }) { StatusCode = StatusCodes.Status403Forbidden },
            _ => new ObjectResult(
                new { code = error.Code, message = error.Message }) { StatusCode = StatusCodes.Status500InternalServerError }
        };
    }

    /// <summary>
    /// Igual que <see cref="ToHttpResult{T}"/> pero para endpoints que devuelven 204.
    /// </summary>
    /// <param name="error">Error de dominio tipado.</param>
    /// <returns>Respuesta HTTP equivalente.</returns>
    public static ActionResult ToHttpResult(this DomainError error) {
        return error switch {
            NotFoundError e => new NotFoundObjectResult(
                new { code = e.Code, message = e.Message }),
            ValidationError e => new BadRequestObjectResult(
                new { code = e.Code, message = e.Message, details = e.ValidationErrors }),
            ConflictError e => new ConflictObjectResult(
                new { code = e.Code, message = e.Message }),
            BusinessRuleError e => new ObjectResult(
                new { code = e.Code, message = e.Message }) { StatusCode = StatusCodes.Status422UnprocessableEntity },
            UnauthorizedError e => new UnauthorizedObjectResult(
                new { code = e.Code, message = e.Message }),
            ForbiddenError e => new ObjectResult(
                new { code = e.Code, message = e.Message }) { StatusCode = StatusCodes.Status403Forbidden },
            _ => new ObjectResult(
                new { code = error.Code, message = error.Message }) { StatusCode = StatusCodes.Status500InternalServerError }
        };
    }
}
