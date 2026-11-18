using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Marca;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Dto.Catalogo_Vehiculos.Marca;

[TestFixture]
[Category("Unit")]
public class MarcaDtoTests {
    [Test]
    public void Constructor_SinDatos_InicializaCadenasYColeccionVacias() {
        // Arrange
        // No se necesitan datos previos.
        // Act
        var dto = new MarcaDto();
        // Assert
        dto.Nombre.Should().BeEmpty();
        dto.Pais.Should().BeEmpty();
        dto.Web.Should().BeEmpty();
        dto.ModelosDisponibles.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public void Propiedades_AsignarDatos_ConservaValores() {
        // Arrange
        var dto = new MarcaDto();
        // Act
        dto.Nombre = "Toyota";
        dto.Pais = "Japón";
        dto.Web = "https://toyota.example.com";
        dto.ModelosDisponibles = new List<string> { "Corolla", "Yaris" };
        // Assert
        dto.Nombre.Should().Be("Toyota");
        dto.Pais.Should().Be("Japón");
        dto.Web.Should().Be("https://toyota.example.com");
        dto.ModelosDisponibles.Should().Equal("Corolla", "Yaris");
    }

    [Test]
    public void ModelosDisponibles_AgregarModelo_NoModificaOtraInstancia() {
        // Arrange
        var dto = new MarcaDto();
        var otro = new MarcaDto();
        // Act
        dto.ModelosDisponibles.Add("Corolla");
        // Assert
        dto.ModelosDisponibles.Should().Equal("Corolla");
        otro.ModelosDisponibles.Should().BeEmpty();
    }
}
