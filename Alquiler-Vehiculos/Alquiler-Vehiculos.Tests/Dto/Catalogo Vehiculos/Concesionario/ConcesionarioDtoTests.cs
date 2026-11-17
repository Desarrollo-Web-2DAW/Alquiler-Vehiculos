using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Concesionario;

namespace Alquiler_Vehiculos.Tests.Dto.Catalogo_Vehiculos.Concesionario;

[TestFixture]
public class ConcesionarioDtoTests {
    [Test]
    public void Constructor_SinDatos_InicializaCadenasVacias() {
        // Arrange & Act
        var dto = new ConcesionarioDto();

        // Assert
        using (Assert.EnterMultipleScope()) {
            Assert.That(dto.Nombre, Is.Empty);
            Assert.That(dto.Ciudad, Is.Empty);
            Assert.That(dto.Telefono, Is.Empty);
            Assert.That(dto.Email, Is.Empty);
        }
    }

    [Test]
    public void Propiedades_AsignarDatos_ConservaValores() {
        // Arrange
        var dto = new ConcesionarioDto();

        // Act
        dto.Nombre = "Madrid Centro";
        dto.Ciudad = "Madrid";
        dto.Telefono = "910123456";
        dto.Email = "centro@example.com";

        // Assert
        using (Assert.EnterMultipleScope()) {
            Assert.That(dto.Nombre, Is.EqualTo("Madrid Centro"));
            Assert.That(dto.Ciudad, Is.EqualTo("Madrid"));
            Assert.That(dto.Telefono, Is.EqualTo("910123456"));
            Assert.That(dto.Email, Is.EqualTo("centro@example.com"));
        }
    }
}
