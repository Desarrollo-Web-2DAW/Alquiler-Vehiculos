using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Concesionario;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Dto.Catalogo_Vehiculos.Concesionario;

/// <summary>
/// Tests unitarios de <see cref="UpdateRequestConcesionarioDto"/>.
/// </summary>
public class UpdateRequestConcesionarioDtoTests
{
    [Test]
    public void Constructor_SinDatos_InicializaCadenasVacias()
    {
        // Arrange & Act
        var dto = new UpdateRequestConcesionarioDto();

        // Assert
        dto.Should().BeEquivalentTo(new
        {
            Nombre = string.Empty,
            Ciudad = string.Empty,
            Telefono = string.Empty,
            Email = string.Empty
        });
    }

    [Test]
    public void Propiedades_AsignarDatos_ConservaValores()
    {
        // Arrange
        var dto = new UpdateRequestConcesionarioDto
        {
            Nombre = "Madrid Centro",
            Ciudad = "Madrid",
            Telefono = "910123456",
            Email = "centro@example.com"
        };

        // Act & Assert
        dto.Should().BeEquivalentTo(new
        {
            Nombre = "Madrid Centro",
            Ciudad = "Madrid",
            Telefono = "910123456",
            Email = "centro@example.com"
        });
    }
}
