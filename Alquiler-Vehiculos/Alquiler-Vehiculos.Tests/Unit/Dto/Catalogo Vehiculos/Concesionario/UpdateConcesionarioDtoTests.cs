using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Concesionario;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Dto.Catalogo_Vehiculos.Concesionario;

[TestFixture]
[Category("Unit")]
public class UpdateConcesionarioDtoTests {
    [Test]
    public void Constructor_SinDatos_InicializaCadenasVacias() {
        // Arrange
        // No se necesitan datos previos.
        // Act
        var dto = new UpdateConcesionarioDto();
        // Assert
        dto.Nombre.Should().BeEmpty();
        dto.Ciudad.Should().BeEmpty();
        dto.Telefono.Should().BeEmpty();
        dto.Email.Should().BeEmpty();
    }

    [Test]
    public void Propiedades_AsignarDatos_ConservaValores() {
        // Arrange
        var dto = new UpdateConcesionarioDto();
        // Act
        dto.Nombre = "Madrid Centro";
        dto.Ciudad = "Madrid";
        dto.Telefono = "910123456";
        dto.Email = "centro@example.com";
        // Assert
        dto.Nombre.Should().Be("Madrid Centro");
        dto.Ciudad.Should().Be("Madrid");
        dto.Telefono.Should().Be("910123456");
        dto.Email.Should().Be("centro@example.com");
    }
}
