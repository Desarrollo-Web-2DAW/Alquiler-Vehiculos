using Alquiler_Vehiculos.Errors.Common;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Errors.Common;

[TestFixture]
[Category("Unit")]
public class DomainErrorTests {
    private sealed record ErrorDePrueba(string Code, string Message) : DomainError(Code, Message) {
        public override string ToString() => base.ToString();
    }

    private static IEnumerable<DomainError> Errores() {
        yield return ValidationError.Create("mensaje");
        yield return new BusinessRuleError("REGLA", "mensaje");
        yield return new ConflictError("CONFLICTO", "mensaje");
        yield return new ForbiddenError("PROHIBIDO", "mensaje");
        yield return new InternalError("mensaje");
        yield return new NotFoundError("NO_ENCONTRADO", "mensaje");
        yield return new UnauthorizedError("NO_AUTORIZADO", "mensaje");
    }

    [TestCaseSource(nameof(Errores))]
    public void Constructor_MensajePersonalizado_ConservaMensajeYTipo(DomainError error) {
        // Arrange
        const string esperado = "mensaje";

        // Act
        var mensaje = error.Message;

        // Assert
        mensaje.Should().Be(esperado);
        error.Code.Should().NotBeNullOrWhiteSpace();
        error.Should().BeAssignableTo<DomainError>();
    }

    [TestCase("")]
    [TestCase("Fallo de prueba")]
    public void ToString_ErrorDerivado_FormateaCodigoYMensaje(string mensaje) {
        // Arrange
        DomainError error = new ErrorDePrueba("PRUEBA", mensaje);

        // Act
        var resultado = error.ToString();

        // Assert
        resultado.Should().Be($"PRUEBA: {mensaje}");
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
        var distintoMensaje = error == new BusinessRuleError("ERROR_INTERNO", "otra regla");
        var distintoCodigo = error == new BusinessRuleError("OTRO", "regla");

        // Assert
        distintoTipo.Should().BeFalse();
        distintoMensaje.Should().BeFalse();
        distintoCodigo.Should().BeFalse();
    }

    [TestCase("id", "Vehiculo con ID '00000000-0000-0000-0000-000000000007' no encontrado")]
    [TestCase("nombre", "Vehiculo con nombre 'Corolla' no encontrado")]
    [TestCase("email", "Vehiculo con email 'cliente@example.com' no encontrado")]
    public void NotFound_Factorias_ConservaCodigoYDevuelveMensajeEsperado(string metodo, string mensaje) {
        // Arrange
        const string recurso = "Vehiculo";
        const string codigo = "VEHICULO_NO_ENCONTRADO";
        var id = Guid.Parse("00000000-0000-0000-0000-000000000007");

        // Act
        var error = metodo switch {
            "id" => NotFoundError.FromId(id, recurso, codigo),
            "nombre" => NotFoundError.FromName("Corolla", recurso, codigo),
            _ => NotFoundError.FromEmail("cliente@example.com", recurso, codigo)
        };

        // Assert
        error.Message.Should().Be(mensaje);
        error.Code.Should().Be(codigo);
    }

    [Test]
    public void Create_SinErroresDeCampo_ConservaMensajeYDiccionarioVacio() {
        // Arrange
        const string mensaje = "Datos incorrectos";

        // Act
        var error = ValidationError.Create(mensaje);

        // Assert
        error.Message.Should().Be(mensaje);
        error.Code.Should().Be("VALIDACION");
        error.ValidationErrors.Should().BeEmpty();
    }

    [Test]
    public void WithFieldErrors_ErroresDeCampos_ConservaDiccionario() {
        // Arrange
        var campos = new Dictionary<string, string[]> { ["Foto"] = ["URL incorrecta"] };

        // Act
        var error = ValidationError.WithFieldErrors(campos);

        // Assert
        error.Message.Should().Be("Errores de validación");
        error.Code.Should().Be("VALIDACION");
        error.ValidationErrors.Should().BeSameAs(campos);
        error.ValidationErrors["Foto"].Should().Equal("URL incorrecta");
    }

    [TestCaseSource(nameof(Errores))]
    public void Copia_ConCodigoPersonalizado_ConservaCodigoYMensaje(DomainError original) {
        // Arrange
        const string codigo = "PERSONALIZADO";

        // Act
        var copia = original with { Code = codigo };

        // Assert
        copia.Code.Should().Be(codigo);
        copia.Message.Should().Be(original.Message);
        original.Code.Should().NotBe(codigo);
    }
}
