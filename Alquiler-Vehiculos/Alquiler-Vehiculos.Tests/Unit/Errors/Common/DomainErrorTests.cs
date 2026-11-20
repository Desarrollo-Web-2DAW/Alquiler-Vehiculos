using Alquiler_Vehiculos.Errors.Common;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Errors.Common;

[TestFixture]
[Category("Unit")]
public class DomainErrorTests {
    // DomainError es abstracto: este tipo permite comprobar su implementación de ToString.
    private sealed record ErrorDePrueba(string Message) : DomainError(Message) {
        public override string ToString() => base.ToString();
    }

    private static IEnumerable<DomainError> Errores() {
        yield return new BadRequestError("mensaje");
        yield return new BusinessRuleError("mensaje");
        yield return new ConflictError("mensaje");
        yield return new ForbiddenError("mensaje");
        yield return new InternalError("mensaje");
        yield return new NotFoundError("mensaje");
        yield return new UnauthorizedError("mensaje");
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

    [TestCase("")]
    [TestCase("Fallo de prueba")]
    public void ToString_ErrorDerivado_FormateaTipoYMensaje(string mensaje) {
        // Arrange
        DomainError error = new ErrorDePrueba(mensaje);

        // Act
        var resultado = error.ToString();

        // Assert
        resultado.Should().Be($"ErrorDePrueba: {mensaje}");
    }

    [Test]
    public void Igualdad_MismoTipoYMensaje_SonIguales() {
        // Arrange
        var primero = new BusinessRuleError("regla");
        var segundo = new BusinessRuleError("regla");

        // Act
        var iguales = primero == segundo;

        // Assert
        iguales.Should().BeTrue();
    }

    [Test]
    public void Igualdad_DistintoTipoOMensaje_NoSonIguales() {
        // Arrange
        DomainError error = new BusinessRuleError("regla");

        // Act
        var distintoTipo = error == new InternalError("regla");
        var distintoMensaje = error == new BusinessRuleError("otra regla");

        // Assert
        distintoTipo.Should().BeFalse();
        distintoMensaje.Should().BeFalse();
    }

    [TestCase("id", false, "VehiculoModel con ID 7 no encontrado")]
    [TestCase("id", true, "Unknown con ID 7 no encontrado")]
    [TestCase("nombre", false, "VehiculoModel con nombre Corolla no encontrado")]
    [TestCase("nombre", true, "Unknown con nombre Corolla no encontrado")]
    [TestCase("objectId", false, "VehiculoModel con ID abc123 no encontrado")]
    [TestCase("objectId", true, "Unknown con ID abc123 no encontrado")]
    public void NotFound_Factorias_DevuelveMensajeEsperado(string metodo, bool porDefecto, string mensaje) {
        // Arrange
        const string recurso = "VehiculoModel";

        // Act
        var error = metodo switch {
            "id" => porDefecto ? NotFoundError.FromId(7) : NotFoundError.FromId(7, recurso),
            "nombre" => porDefecto ? NotFoundError.FromName("Corolla") : NotFoundError.FromName("Corolla", recurso),
            _ => porDefecto ? NotFoundError.FromObjectId("abc123") : NotFoundError.FromObjectId("abc123", recurso)
        };

        // Assert
        error.Message.Should().Be(mensaje);
    }

    [Test]
    public void ValidationError_SinErroresDeCampo_ConservaMensajeYDiccionarioNulo() {
        // Arrange
        const string mensaje = "Datos incorrectos";

        // Act
        var error = new BadRequestError.ValidationError(mensaje);

        // Assert
        error.Message.Should().Be(mensaje);
        error.ValidationErrors.Should().BeNull();
    }

    [Test]
    public void WithFieldErrors_ErroresDeCampos_ConservaDiccionario() {
        // Arrange
        var campos = new Dictionary<string, string[]> { ["Foto"] = ["URL incorrecta"] };

        // Act
        var error = BadRequestError.ValidationError.WithFieldErrors(campos);

        // Assert
        error.Message.Should().Be("Errores de validación");
        error.ValidationErrors.Should().BeSameAs(campos);
        error.ValidationErrors!["Foto"].Should().Equal("URL incorrecta");
    }

    [TestCase(false, "Credenciales inválidas")]
    [TestCase(true, "Token expirado o inválido")]
    public void Unauthorized_Factorias_DevuelveMensajeEsperado(bool token, string mensaje) {
        // Arrange
        // No requiere dependencias.

        // Act
        var error = token ? UnauthorizedError.TokenExpired() : UnauthorizedError.InvalidCredentials();

        // Assert
        error.Message.Should().Be(mensaje);
    }

    [Test]
    public void NotOwner_RecursoEIdentificador_IncluyeAmbosEnMensaje() {
        // Arrange
        const string recurso = "vehiculoModel";
        const string id = "7";

        // Act
        var error = ForbiddenError.NotOwner(recurso, id);

        // Assert
        error.Message.Should().Be("No tienes permisos para acceder a este vehiculoModel (ID: 7)");
    }

    [TestCase(false, "Ya existe un VehiculoModel con el valor '1234BCD'")]
    [TestCase(true, "No se puede borrar VehiculoModel con el valor '1234BCD'")]
    public void Conflict_Factorias_IncluyeRecursoYValor(bool dependencia, string mensaje) {
        // Arrange
        const string recurso = "VehiculoModel";
        const string valor = "1234BCD";

        // Act
        var error = dependencia ? ConflictError.Dependency(recurso, valor) : ConflictError.Duplicate(recurso, valor);

        // Assert
        error.Message.Should().Be(mensaje);
    }
}
