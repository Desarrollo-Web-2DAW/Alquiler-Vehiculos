using Alquiler_Vehiculos.Models.Vehiculo;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Models;

/// <summary>
/// Tests unitarios del record <see cref="Marca"/>.
/// Al ser un record posicional, la igualdad es estructural: dos instancias con
/// los mismos valores son iguales aunque no compartan referencia.
/// </summary>
public class MarcaTests
{
    private static readonly DateTime Fecha = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
    private static Marca CrearMarca() => new(1, "Toyota", "Japón", "https://www.toyota.es", false, Fecha, null, null);

    [Test]
    public void Constructor_ConTodosLosValores_AsignaCadaPropiedad()
    {
        // Arrange & Act
        var marca = CrearMarca();

        // Assert
        marca.Should().BeEquivalentTo(new
        {
            Id = 1,
            Nombre = "Toyota",
            Pais = "Japón",
            Web = "https://www.toyota.es"
        });
    }

    [Test]
    public void Constructor_ConCadenasVacias_ConservaLasCadenasVacias()
    {
        // Arrange & Act
        var marca = new Marca(0, string.Empty, string.Empty, string.Empty, false, Fecha, null, null);

        // Assert
        marca.Should().BeEquivalentTo(new
        {
            Id = 0,
            Nombre = string.Empty,
            Pais = string.Empty,
            Web = string.Empty
        });
    }

    [Test]
    public void Constructor_ConAuditoria_ConservaLosValores()
    {
        // Arrange & Act
        var marca = new Marca(1, "Toyota", "Japón", "https://www.toyota.es", true, Fecha, Fecha.AddDays(1), Fecha.AddDays(2));

        // Assert
        marca.IsDeleted.Should().BeTrue();
        marca.CreatedAt.Should().Be(Fecha);
        marca.UpdatedAt.Should().Be(Fecha.AddDays(1));
        marca.DeletedAt.Should().Be(Fecha.AddDays(2));
    }

    [Test]
    public void Igualdad_DosInstanciasConMismosValores_SonIguales()
    {
        // Arrange
        var marca = CrearMarca();
        var otraMarca = CrearMarca();

        // Act & Assert
        marca.Should().Be(otraMarca);
    }

    [Test]
    public void Igualdad_ConDistintoId_NoSonIguales()
    {
        // Arrange
        var marca = CrearMarca();
        var otraMarca = CrearMarca() with { Id = 2 };

        // Act & Assert
        marca.Should().NotBe(otraMarca);
    }

    [Test]
    public void Igualdad_ConDistintoNombre_NoSonIguales()
    {
        // Arrange
        var marca = CrearMarca();
        var otraMarca = CrearMarca() with { Nombre = "Honda" };

        // Act & Assert
        marca.Should().NotBe(otraMarca);
    }

    [Test]
    public void Igualdad_ConDistintoPais_NoSonIguales()
    {
        // Arrange
        var marca = CrearMarca();
        var otraMarca = CrearMarca() with { Pais = "Alemania" };

        // Act & Assert
        marca.Should().NotBe(otraMarca);
    }

    [Test]
    public void Igualdad_ConDistintaWeb_NoSonIguales()
    {
        // Arrange
        var marca = CrearMarca();
        var otraMarca = CrearMarca() with { Web = "https://www.honda.es" };

        // Act & Assert
        marca.Should().NotBe(otraMarca);
    }

    [Test]
    public void Equals_ConNull_DevuelveFalse()
    {
        // Arrange
        var marca = CrearMarca();

        // Act
        var sonIguales = marca.Equals((object?)null);

        // Assert
        sonIguales.Should().BeFalse();
    }

    [Test]
    public void Equals_ConObjetoDeOtroTipo_DevuelveFalse()
    {
        // Arrange
        var marca = CrearMarca();

        // Act
        var sonIguales = marca.Equals((object)"Toyota");

        // Assert
        sonIguales.Should().BeFalse();
    }

    [Test]
    public void GetHashCode_DosInstanciasConMismosValores_DevuelveElMismoHash()
    {
        // Arrange
        var marca = CrearMarca();
        var otraMarca = CrearMarca();

        // Act
        var hash = marca.GetHashCode();
        var otroHash = otraMarca.GetHashCode();

        // Assert
        hash.Should().Be(otroHash);
    }

    [Test]
    public void With_DevuelveNuevaInstanciaConLosCambios()
    {
        // Arrange
        var original = CrearMarca();

        // Act
        var modificada = original with { Nombre = "Honda" };

        // Assert
        modificada.Should().BeEquivalentTo(new
        {
            Id = 1L,
            Nombre = "Honda",
            Pais = "Japón",
            Web = "https://www.toyota.es"
        });
    }

    [Test]
    public void With_NoModificaLaInstanciaOriginal()
    {
        // Arrange
        var original = CrearMarca();

        // Act
        _ = original with { Nombre = "Honda" };

        // Assert
        original.Nombre.Should().Be("Toyota");
    }

    [Test]
    public void ToString_ContieneTodosLosValores()
    {
        // Arrange
        var marca = CrearMarca();

        // Act
        var texto = marca.ToString();

        // Assert
        texto.Should().ContainAll("1", "Toyota", "Japón", "https://www.toyota.es");
    }
}
