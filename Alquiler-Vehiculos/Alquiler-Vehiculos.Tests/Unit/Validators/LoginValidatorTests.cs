using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Validators.Usuarios;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators;

/// <summary>
/// Tests unitarios del validador de login.
/// Comprueba las reglas de formato de email y contraseña.
/// </summary>
[Category("Unit")]
public class LoginValidatorTests {
    private LoginValidator _validator = null!;

    [SetUp]
    public void Setup() {
        _validator = new LoginValidator();
    }

    #region Email

    /// <summary>
    /// Dado un LoginDto con email y contraseña válidos,
    /// cuando se valida, entonces la validación es correcta.
    /// </summary>
    [Test]
    public async Task ValidateAsync_DatosValidos_EsValido() {
        // Arrange
        var dto = new LoginDto { Email = "usuario@ejemplo.com", Password = "Contraseña1!" };

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Dado un LoginDto con email vacío, cuando se valida,
    /// entonces falla con error en el campo Email.
    /// </summary>
    [Test]
    public async Task ValidateAsync_EmailVacio_FallaConErrorEmail() {
        // Arrange
        var dto = new LoginDto { Email = "", Password = "Contraseña1!" };

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    /// <summary>
    /// Dado un LoginDto con email sin formato válido, cuando se valida,
    /// entonces falla con error de formato de email.
    /// </summary>
    [Test]
    public async Task ValidateAsync_EmailSinFormato_FallaConErrorFormato() {
        // Arrange
        var dto = new LoginDto { Email = "no-es-un-email", Password = "Contraseña1!" };

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    #endregion

    #region Password

    /// <summary>
    /// Dado un LoginDto con contraseña vacía, cuando se valida,
    /// entonces falla con error en el campo Password.
    /// </summary>
    [Test]
    public async Task ValidateAsync_ContraseñaVacia_FallaConErrorPassword() {
        // Arrange
        var dto = new LoginDto { Email = "usuario@ejemplo.com", Password = "" };

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle(e => e.PropertyName == "Password");
    }

    #endregion

    #region Múltiples errores

    /// <summary>
    /// Dado un LoginDto con email y contraseña vacíos, cuando se valida,
    /// entonces falla con errores en ambos campos.
    /// </summary>
    [Test]
    public async Task ValidateAsync_AmbosCamposVacios_FallaConDosErrores() {
        // Arrange
        var dto = new LoginDto { Email = "", Password = "" };

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
        resultado.Errors.Select(e => e.PropertyName).Should().Contain("Email");
        resultado.Errors.Select(e => e.PropertyName).Should().Contain("Password");
    }

    #endregion
}
