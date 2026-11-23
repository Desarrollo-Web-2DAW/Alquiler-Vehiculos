using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Validators.Usuarios;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators;

/// <summary>
/// Pruebas unitarias de <see cref="CambiarRolValidator"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
public class CambiarRolValidatorTests {
    private CambiarRolValidator _validator = null!;

    [SetUp]
    public void SetUp() {
        _validator = new CambiarRolValidator();
    }

    [TestCase("Cliente")]
    [TestCase("Trabajador")]
    [TestCase("Responsable")]
    [TestCase("Admin")]
    public async Task ValidateAsync_RolValido_EsValido(string rol) {
        // Arrange
        var dto = new CambiarRolDto { Role = rol };

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ValidateAsync_RolVacio_FallaConErrorRole() {
        // Arrange
        var dto = new CambiarRolDto { Role = "" };

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CambiarRolDto.Role));
    }

    [Test]
    public async Task ValidateAsync_RolInexistente_FallaConErrorRole() {
        // Arrange
        var dto = new CambiarRolDto { Role = "SuperAdmin" };

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CambiarRolDto.Role));
    }
}
