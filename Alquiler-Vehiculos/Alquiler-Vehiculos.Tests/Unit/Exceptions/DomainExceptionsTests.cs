using FluentAssertions;
using General = Alquiler_Vehiculos.Exceptions;

namespace Alquiler_Vehiculos.Tests.Unit.Exceptions;

[TestFixture]
[Category("Unit")]
public class DomainExceptionsTests {
    public static IEnumerable<TestCaseData> Constructores() {
        yield return new TestCaseData((Func<string, Exception>) (m => new General.BusinessException(m)), (Func<string, Exception, Exception>) ((m, e) => new General.BusinessException(m, e)));
        yield return new TestCaseData((Func<string, Exception>) (m => new General.NotFoundException(m)), (Func<string, Exception, Exception>) ((m, e) => new General.NotFoundException(m, e)));
        yield return new TestCaseData((Func<string, Exception>) (m => new General.SerializationFailureException(m)), (Func<string, Exception, Exception>) ((m, e) => new General.SerializationFailureException(m, e)));
        yield return new TestCaseData((Func<string, Exception>) (m => new General.ValidationException(m)), (Func<string, Exception, Exception>) ((m, e) => new General.ValidationException(m, e)));
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

    [Test]
    public void ValidationException_ConDetalleOSinDetalle_ConservaErrores() {
        // Arrange
        var errores = new Dictionary<string, string[]> { ["Nombre"] = ["Obligatorio"] };
        var causa = new Exception("Origen");

        // Act
        var simples = new General.ValidationException("Error").Errors;
        var internos = new General.ValidationException("Error", causa).Errors;
        var detallada = new General.ValidationException("Error", errores).Errors;

        // Assert
        simples.Should().BeEmpty();
        internos.Should().BeEmpty();
        detallada.Should().BeSameAs(errores);
    }
}
