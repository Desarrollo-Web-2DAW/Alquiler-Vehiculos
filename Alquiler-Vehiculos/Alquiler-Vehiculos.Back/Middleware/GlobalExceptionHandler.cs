using System.Text.Json;
using System.Text.Json.Serialization;
using Alquiler_Vehiculos.Exceptions;

namespace Alquiler_Vehiculos.Middleware;

/// <summary>
///     Middleware encargado de capturar las excepciones no controladas
///     durante el procesamiento de las peticiones HTTP y transformarlas
///     en respuestas JSON con el código de estado correspondiente.
/// </summary>
/// <param name="next">Delegado que permite ejecutar el siguiente middleware del pipeline.</param>
/// <param name="logger">Servicio de registro utilizado para almacenar información sobre las excepciones.</param>
public class GlobalExceptionHandler(
    RequestDelegate next,
    ILogger<GlobalExceptionHandler> logger
) {
    /// <summary>
    ///     Ejecuta el siguiente middleware y captura cualquier excepción
    ///     que no haya sido gestionada durante el procesamiento de la petición.
    /// </summary>
    /// <param name="context">Contexto HTTP de la petición actual.</param>
    public async Task InvokeAsync(HttpContext context) {
        try {
            await next(context);
        }
        catch (Exception ex) {
            var errorId = Guid.NewGuid().ToString()[..8];

            logger.LogError(ex, "Excepción no manejada. ErrorId: {ErrorId}, Message: {Message}",
                errorId, ex.Message);

            await HandleExceptionAsync(context, ex, errorId);
        }
    }

    /// <summary>
    ///     Determina el código HTTP y la información correspondiente a la excepción
    ///     recibida, y genera una respuesta JSON con los detalles del error.
    /// </summary>
    /// <param name="context">Contexto HTTP de la petición actual.</param>
    /// <param name="exception">Excepción que se ha producido.</param>
    /// <param name="errorId">Identificador único generado para localizar el error en los registros.</param>
    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        string errorId
    ) {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errors, errorType) = exception switch {
            NotFoundException notFound => (
                404,
                notFound.Message,
                null,
                "NotFoundError"
            ),

            ValidationException validation => (
                400,
                validation.Message,
                validation.Errors,
                "ValidationError"
            ),

            BusinessException business => (
                400,
                business.Message,
                null,
                "BusinessRuleError"
            ),

            UnauthorizedAccessException => (
                401,
                "No autorizado",
                null,
                "UnauthorizedError"
            ),

            ArgumentException argument => (
                400,
                argument.Message,
                null,
                "ValidationError"
            ),

            TimeoutException => (
                408,
                "Tiempo de espera agotado",
                null,
                "InternalError"
            ),

            _ => (
                500,
                "Ha ocurrido un error interno",
                (Dictionary<string, string[]>?)null,
                "InternalError"
            )
        };

        context.Response.StatusCode = statusCode;

        // Construye el objeto que contendrá la información de la respuesta.
        var response = new {
            errorId,
            message,
            errorType,
            timestamp = DateTime.UtcNow.ToString("o"),
            path = context.Request.Path.Value,
            method = context.Request.Method,
            errors
        };

        // Configura la serialización JSON para utilizar nombres camelCase
        // y omitir las propiedades cuyo valor sea null.
        var jsonOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

/// <summary>
///     Proporciona métodos de extensión para registrar el middleware
///     GlobalExceptionHandler en el pipeline de ASP.NET Core.
/// </summary>
public static class GlobalExceptionHandlerExtensions {
    /// <summary>
    ///     Añade el middleware GlobalExceptionHandler al pipeline de peticiones HTTP.
    /// </summary>
    /// <param name="app">Aplicación ASP.NET Core en la que se registra el middleware.</param>
    /// <returns>La misma aplicación con el middleware registrado.</returns>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app) {
        return app.UseMiddleware<GlobalExceptionHandler>();
    }
}