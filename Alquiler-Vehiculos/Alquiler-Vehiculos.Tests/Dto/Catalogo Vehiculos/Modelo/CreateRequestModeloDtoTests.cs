using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Modelo;

namespace Alquiler_Vehiculos.Tests.Dto.Catalogo_Vehiculos.Modelo;

[TestFixture]
public class CreateRequestModeloDtoTests {
    [Test]
    public void Constructor_SinDatos_InicializaValoresPorDefecto() {
        // Arrange & Act
        var dto = new CreateRequestModeloDto();

        // Assert
        using (Assert.EnterMultipleScope()) {
            Assert.That(dto.Nombre, Is.Empty);
            Assert.That(dto.Anio, Is.Zero);
            Assert.That(dto.NombreMarca, Is.Empty);
        }
    }

    [Test]
    public void Propiedades_AsignarDatos_ConservaValores() {
        // Arrange
        var dto = new CreateRequestModeloDto();

        // Act
        dto.Nombre = "Corolla";
        dto.Anio = 2025;
        dto.NombreMarca = "Toyota";

        // Assert
        using (Assert.EnterMultipleScope()) {
            Assert.That(dto.Nombre, Is.EqualTo("Corolla"));
            Assert.That(dto.Anio, Is.EqualTo(2025));
            Assert.That(dto.NombreMarca, Is.EqualTo("Toyota"));
        }
    }
}
