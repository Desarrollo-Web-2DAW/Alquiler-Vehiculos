using System.Text.Json;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Alquiler_Vehiculos.Tests.Unit.Extensions;

[TestFixture]
[Category("Unit")]
public class DomainErrorExtensionsTests {
    private sealed record ErrorDesconocido() : DomainError("DESCONOCIDO", "Error de prueba");

    private static IEnumerable<TestCaseData> Casos() {
        var errores = new (DomainError Error, int Estado, Type Tipo)[] {
            (new NotFoundError("NO_EXISTE", "No encontrado"), 404, typeof(NotFoundObjectResult)),
            (ValidationError.Create("Datos incorrectos"), 400, typeof(BadRequestObjectResult)),
            (ValidationError.WithFieldErrors(new() { ["Nombre"] = ["Obligatorio", "Demasiado corto"] }), 400, typeof(BadRequestObjectResult)),
            (new ConflictError("CONFLICTO", "Ya existe"), 409, typeof(ConflictObjectResult)),
            (new BusinessRuleError("REGLA", "Operación no permitida"), 422, typeof(ObjectResult)),
            (new UnauthorizedError("NO_AUTENTICADO", "Identifícate"), 401, typeof(UnauthorizedObjectResult)),
            (new ForbiddenError("SIN_PERMISOS", "Acceso denegado"), 403, typeof(ObjectResult)),
            (new InternalError("Fallo interno"), 500, typeof(ObjectResult)),
            (new ErrorDesconocido(), 500, typeof(ObjectResult))
        };
        foreach (var (error, estado, tipo) in errores) {
            yield return new TestCaseData(error, estado, tipo, false);
            yield return new TestCaseData(error, estado, tipo, true);
        }
    }

    [TestCaseSource(nameof(Casos))]
    public void ToHttpResult_ErrorDeDominio_DevuelveEstadoCodigoMensajeYDetalles(
        DomainError error, int estado, Type tipo, bool generico) {
        // Arrange
        var esperado = error;

        // Act
        var respuesta = generico ? error.ToHttpResult<string>().Result : error.ToHttpResult();

        // Assert
        respuesta.Should().BeOfType(tipo);
        var resultado = (ObjectResult)respuesta!;
        resultado.StatusCode.Should().Be(estado);
        var cuerpo = JsonSerializer.SerializeToElement(resultado.Value);
        cuerpo.GetProperty("code").GetString().Should().Be(esperado.Code);
        cuerpo.GetProperty("message").GetString().Should().Be(esperado.Message);
        if (error is ValidationError validacion) {
            cuerpo.GetProperty("details").Deserialize<Dictionary<string, string[]>>()
                .Should().BeEquivalentTo(validacion.ValidationErrors);
        } else {
            cuerpo.TryGetProperty("details", out _).Should().BeFalse();
        }
    }
}
