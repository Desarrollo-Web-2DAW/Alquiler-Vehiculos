using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Marca;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Dto.Catalogo_Vehiculos.Marca;

/// <summary>
/// Tests unitarios de <see cref="CreateRequestMarcaDto"/>.
/// </summary>
public class CreateRequestMarcaDtoTests
{
    [Test]
    public void Constructor_SinDatos_InicializaCadenasVacias()
    {
        // Arrange & Act
        var dto = new CreateRequestMarcaDto();

        // Assert
        dto.Should().BeEquivalentTo(new
        {
            Nombre = string.Empty,
            Pais = string.Empty,
            Web = string.Empty
        });
    }

    [Test]
    public void Constructor_SinDatos_InicializaColeccionVacia()
    {
        // Arrange & Act
        var dto = new CreateRequestMarcaDto();

        // Assert
        dto.ModelosDisponibles.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public void Propiedades_AsignarDatos_ConservaValores()
    {
        // Arrange
        var dto = new CreateRequestMarcaDto
        {
            Nombre = "Toyota",
            Pais = "Japón",
            Web = "https://toyota.example.com",
            ModelosDisponibles = new List<string> { "Corolla", "Yaris" }
        };

        // Act & Assert
        dto.Should().BeEquivalentTo(new
        {
            Nombre = "Toyota",
            Pais = "Japón",
            Web = "https://toyota.example.com",
            ModelosDisponibles = new[] { "Corolla", "Yaris" }
        });
    }

    [Test]
    public void ModelosDisponibles_AgregarModelo_SeAnadeALaColeccion()
    {
        // Arrange
        var dto = new CreateRequestMarcaDto();

        // Act
        dto.ModelosDisponibles.Add("Corolla");

        // Assert
        dto.ModelosDisponibles.Should().Equal("Corolla");
    }

    [Test]
    public void ModelosDisponibles_AgregarModelo_NoModificaOtraInstancia()
    {
        // Arrange
        var dto = new CreateRequestMarcaDto();
        var otroDto = new CreateRequestMarcaDto();

        // Act
        dto.ModelosDisponibles.Add("Corolla");

        // Assert
        otroDto.ModelosDisponibles.Should().BeEmpty();
    }
}
