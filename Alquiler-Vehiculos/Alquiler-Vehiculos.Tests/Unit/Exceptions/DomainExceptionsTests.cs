using FluentAssertions;
using General = Alquiler_Vehiculos.Exceptions;
using Usuario = Alquiler_Vehiculos.Exceptions.Usuario;

namespace Alquiler_Vehiculos.Tests.Unit.Exceptions;

[TestFixture]
[Category("Unit")]
public class DomainExceptionsTests {
    public static IEnumerable<TestCaseData> Constructores() {
        yield return new TestCaseData((Func<string, Exception>) (m => new General.BusinessException(m)), (Func<string, Exception, Exception>) ((m, e) => new General.BusinessException(m, e)));
        yield return new TestCaseData((Func<string, Exception>) (m => new General.NotFoundException(m)), (Func<string, Exception, Exception>) ((m, e) => new General.NotFoundException(m, e)));
        yield return new TestCaseData((Func<string, Exception>) (m => new General.SerializationFailureException(m)), (Func<string, Exception, Exception>) ((m, e) => new General.SerializationFailureException(m, e)));
        yield return new TestCaseData((Func<string, Exception>) (m => new General.ValidationException(m)), (Func<string, Exception, Exception>) ((m, e) => new General.ValidationException(m, e)));
        yield return new TestCaseData((Func<string, Exception>) (m => new Usuario.BusinessException(m)), (Func<string, Exception, Exception>) ((m, e) => new Usuario.BusinessException(m, e)));
        yield return new TestCaseData((Func<string, Exception>) (m => new Usuario.NotFoundException(m)), (Func<string, Exception, Exception>) ((m, e) => new Usuario.NotFoundException(m, e)));
        yield return new TestCaseData((Func<string, Exception>) (m => new Usuario.SerializationFailureException(m)), (Func<string, Exception, Exception>) ((m, e) => new Usuario.SerializationFailureException(m, e)));
        yield return new TestCaseData((Func<string, Exception>) (m => new Usuario.ValidationException(m)), (Func<string, Exception, Exception>) ((m, e) => new Usuario.ValidationException(m, e)));
    }

    [TestCaseSource(nameof(Constructores))]
    public void Constructor_ConYSinCausa_ConservaMensajeYCausa(Func<string, Exception> simple, Func<string, Exception, Exception> conCausa) {
        // Arrange
        const string mensaje = "Error del dominio";
        var causa = new InvalidOperationException("Causa original");

        // Act
        var primera = simple(mensaje);
        var segunda = conCausa(mensaje, causa);

        // Assert
        primera.Message.Should().Be(mensaje);
        primera.InnerException.Should().BeNull();
        segunda.Message.Should().Be(mensaje);
        segunda.InnerException.Should().BeSameAs(causa);
        segunda.Should().BeOfType(primera.GetType());
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ValidationException_ConDetalleOSinDetalle_ConservaErrores(bool usuario) {
        // Arrange
        var errores = new Dictionary<string, string[]> { ["Nombre"] = ["Obligatorio"] };
        var causa = new Exception("Origen");

        // Act
        var simples = usuario ? new Usuario.ValidationException("Error").Errors : new General.ValidationException("Error").Errors;
        var internos = usuario ? new Usuario.ValidationException("Error", causa).Errors : new General.ValidationException("Error", causa).Errors;
        var detallada = usuario ? new Usuario.ValidationException("Error", errores).Errors : new General.ValidationException("Error", errores).Errors;

        // Assert
        simples.Should().BeEmpty();
        internos.Should().BeEmpty();
        detallada.Should().BeSameAs(errores);
    }
}
