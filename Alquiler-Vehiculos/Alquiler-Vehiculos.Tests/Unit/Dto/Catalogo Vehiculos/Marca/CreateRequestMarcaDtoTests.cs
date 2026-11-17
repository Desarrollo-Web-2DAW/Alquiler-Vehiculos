using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Marca;

namespace Alquiler_Vehiculos.Tests.Dto.Catalogo_Vehiculos.Marca;

[TestFixture]
public class CreateRequestMarcaDtoTests {
    [Test]
    public void Constructor_SinDatos_InicializaCadenasYColeccionVacias() {
        // Arrange & Act
        var dto = new CreateRequestMarcaDto();

        // Assert
        using (Assert.EnterMultipleScope()) {
            Assert.That(dto.Nombre, Is.Empty);
            Assert.That(dto.Pais, Is.Empty);
            Assert.That(dto.Web, Is.Empty);
            Assert.That(dto.ModelosDisponibles, Is.Not.Null.And.Empty);
        }
    }

    [Test]
    public void Propiedades_AsignarDatos_ConservaValores() {
        // Arrange
        var dto = new CreateRequestMarcaDto();

        // Act
        dto.Nombre = "Toyota";
        dto.Pais = "Japón";
        dto.Web = "https://toyota.example.com";
        dto.ModelosDisponibles = new List<string> { "Corolla", "Yaris" };

        // Assert
        using (Assert.EnterMultipleScope()) {
            Assert.That(dto.Nombre, Is.EqualTo("Toyota"));
            Assert.That(dto.Pais, Is.EqualTo("Japón"));
            Assert.That(dto.Web, Is.EqualTo("https://toyota.example.com"));
            Assert.That(dto.ModelosDisponibles, Is.EqualTo(new[] { "Corolla", "Yaris" }));
        }
    }

    [Test]
    public void ModelosDisponibles_AgregarModelo_NoModificaOtraInstancia() {
        // Arrange
        var dto = new CreateRequestMarcaDto();
        var otroDto = new CreateRequestMarcaDto();

        // Act
        dto.ModelosDisponibles.Add("Corolla");

        // Assert
        using (Assert.EnterMultipleScope()) {
            Assert.That(dto.ModelosDisponibles, Is.EqualTo(new[] { "Corolla" }));
            Assert.That(otroDto.ModelosDisponibles, Is.Empty);
        }
    }
}
