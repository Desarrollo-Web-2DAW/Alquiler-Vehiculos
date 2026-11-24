using Alquiler_Vehiculos.Dto.Oficina;
using Alquiler_Vehiculos.Validators.Oficina;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators;

/// <summary>
/// Pruebas unitarias de <see cref="CambiarResponsableValidator"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
public class OficinaValidatorsTests {
    private CambiarResponsableValidator _responsableValidator = null!;

    [SetUp]
    public void SetUp() {
        _responsableValidator = new CambiarResponsableValidator();
    }

    [Test]
    public async Task CambiarResponsable_GuidVacio_Falla() {
        // Act
        var resultado = await _responsableValidator.ValidateAsync(new CambiarResponsableDto { TrabajadorId = Guid.Empty });

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CambiarResponsableDto.TrabajadorId));
    }

    [Test]
    public async Task CambiarResponsable_GuidValido_EsValido() {
        // Act
        var resultado = await _responsableValidator.ValidateAsync(new CambiarResponsableDto { TrabajadorId = Guid.NewGuid() });

        // Assert
        resultado.IsValid.Should().BeTrue();
    }
}
