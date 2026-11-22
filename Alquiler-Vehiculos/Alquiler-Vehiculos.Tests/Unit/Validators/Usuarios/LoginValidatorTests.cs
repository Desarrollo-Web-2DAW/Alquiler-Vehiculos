using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Validators.Usuarios;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators.Usuarios;

[TestFixture]
[Category("Unit")]
public class LoginValidatorTests {
    [Test]
    public void Validate_CredencialesValidas_NoDevuelveErrores() {
        // Arrange
        var validator = new LoginValidator();
        var dto = new LoginDto { Email = "cliente@example.com", Password = "clave" };

        // Act
        var resultado = validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [TestCase("", "clave", "Email", "El email es obligatorio")]
    [TestCase("incorrecto", "clave", "Email", "Debe ser un email válido")]
    [TestCase("cliente@example.com", "", "Password", "La contraseña es obligatoria")]
    public void Validate_CredencialesInvalidas_DevuelveErrorDelCampo(
        string email, string password, string campo, string mensaje) {
        // Arrange
        var validator = new LoginValidator();
        var dto = new LoginDto { Email = email, Password = password };

        // Act
        var resultado = validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == campo && e.ErrorMessage == mensaje);
    }
}
