using Alquiler_Vehiculos.Errors.Common;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Errors.Common;

[TestFixture]
[Category("Unit")]
public class DomainErrorTests {
    // DomainError es abstracto: este tipo permite comprobar su implementación de ToString.
    private sealed record ErrorDePrueba(string Code, string Message) : DomainError(Code, Message) {
        public override string ToString() => base.ToString();
    }

    private static IEnumerable<DomainError> Errores() {
        yield return ValidationError.Create("mensaje");
        yield return new BusinessRuleError("REGLA", "mensaje");
        yield return new ConflictError("CONFLICTO", "mensaje");
        yield return new ForbiddenError("SIN_PERMISOS", "mensaje");
        yield return new InternalError("mensaje");
        yield return new NotFoundError("NO_ENCONTRADO", "mensaje");
        yield return new UnauthorizedError("NO_AUTENTICADO", "mensaje");
    }

    [TestCaseSource(nameof(Errores))]
    public void Constructor_MensajePersonalizado_ConservaMensajeYTipo(DomainError error) {
        // Arrange
        const string esperado = "mensaje";

        // Act
        var mensaje = error.Message;

        // Assert
        mensaje.Should().Be(esperado);
        error.Should().BeAssignableTo<DomainError>();
    }

    [TestCase("REGLA", "")]
    [TestCase("ERROR_PRUEBA", "Fallo de prueba")]
    public void ToString_ErrorDerivado_FormateaCodigoYMensaje(string codigo, string mensaje) {
        // Arrange
        DomainError error = new ErrorDePrueba(codigo, mensaje);

        // Act
        var resultado = error.ToString();

        // Assert
        resultado.Should().Be($"{codigo}: {mensaje}");
    }

    [Test]
    public void Igualdad_MismoTipoCodigoYMensaje_SonIguales() {
        // Arrange
        var primero = new BusinessRuleError("REGLA", "regla");
        var segundo = new BusinessRuleError("REGLA", "regla");

        // Act
        var iguales = primero == segundo;

        // Assert
        iguales.Should().BeTrue();
    }

    [Test]
    public void Igualdad_DistintoTipoCodigoOMensaje_NoSonIguales() {
        // Arrange
        DomainError error = new BusinessRuleError("ERROR_INTERNO", "regla");

        // Act
        var distintoTipo = error == new InternalError("regla");
        var distintoCodigo = error == new BusinessRuleError("OTRA_REGLA", "regla");
        var distintoMensaje = error == new BusinessRuleError("ERROR_INTERNO", "otra regla");

        // Assert
        distintoTipo.Should().BeFalse();
        distintoCodigo.Should().BeFalse();
        distintoMensaje.Should().BeFalse();
    }

    [TestCase("id", "Vehiculo con ID '00000000-0000-0000-0000-000000000007' no encontrado")]
    [TestCase("nombre", "Vehiculo con nombre 'Corolla' no encontrado")]
    [TestCase("email", "Vehiculo con email 'prueba@example.com' no encontrado")]
    public void NotFound_Factorias_DevuelveCodigoYMensajeEsperados(string metodo, string mensaje) {
        // Arrange
        const string recurso = "Vehiculo";
        const string codigo = "VEHICULO_NO_ENCONTRADO";
        var id = Guid.Parse("00000000-0000-0000-0000-000000000007");

        // Act
        var error = metodo switch {
            "id" => NotFoundError.FromId(id, recurso, codigo),
            "nombre" => NotFoundError.FromName("Corolla", recurso, codigo),
            _ => NotFoundError.FromEmail("prueba@example.com", recurso, codigo)
        };

        // Assert
        error.Code.Should().Be(codigo);
        error.Message.Should().Be(mensaje);
    }

    [Test]
    public void ValidationError_SinErroresDeCampo_ConservaMensajeYDiccionarioVacio() {
        // Arrange
        const string mensaje = "Datos incorrectos";

        // Act
        var error = ValidationError.Create(mensaje);

        // Assert
        error.Code.Should().Be("VALIDACION");
        error.Message.Should().Be(mensaje);
        error.ValidationErrors.Should().BeEmpty();
    }

    [Test]
    public void WithFieldErrors_ErroresDeCampos_ConservaDiccionario() {
        // Arrange
        var campos = new Dictionary<string, string[]> { ["Foto"] = ["URL incorrecta"] };

        // Act
        var error = ValidationError.WithFieldErrors(campos);

        // Assert
        error.Code.Should().Be("VALIDACION");
        error.Message.Should().Be("Errores de validación");
        error.ValidationErrors.Should().BeSameAs(campos);
        error.ValidationErrors!["Foto"].Should().Equal("URL incorrecta");
    }

    [TestCase("BusinessRule")]
    [TestCase("Conflict")]
    [TestCase("Forbidden")]
    [TestCase("NotFound")]
    [TestCase("Unauthorized")]
    public void Constructor_CodigoPersonalizado_ConservaCodigo(string tipo) {
        // Arrange
        const string codigo = "CODIGO_PRUEBA";
        const string mensaje = "mensaje";

        // Act
        DomainError error = tipo switch {
            "BusinessRule" => new BusinessRuleError(codigo, mensaje),
            "Conflict" => new ConflictError(codigo, mensaje),
            "Forbidden" => new ForbiddenError(codigo, mensaje),
            "NotFound" => new NotFoundError(codigo, mensaje),
            _ => new UnauthorizedError(codigo, mensaje)
        };

        // Assert
        error.Code.Should().Be(codigo);
        error.Message.Should().Be(mensaje);
    }

    [Test]
    public void InternalError_MensajePersonalizado_UtilizaCodigoFijo() {
        // Arrange
        const string mensaje = "Fallo interno";

        // Act
        var error = new InternalError(mensaje);

        // Assert
        error.Code.Should().Be("ERROR_INTERNO");
        error.Message.Should().Be(mensaje);
    }
}
