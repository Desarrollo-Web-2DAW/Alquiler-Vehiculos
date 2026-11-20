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
    private static readonly DateTime FechaCreacion = new(2024, 1, 1);

    private static Marca CrearMarca() =>
        new(1, "Toyota", "Japón", "https://www.toyota.es", false, FechaCreacion, null, null);

    #region Constructor

    [Test]
    public void Constructor_ConTodosLosValores_AsignaCadaPropiedad()
    {
        // Arrange & Act
        var marca = CrearMarca();

        // Assert
        marca.Should().BeEquivalentTo(new
        {
            Id = 1L,
            Nombre = "Toyota",
            Pais = "Japón",
            Web = "https://www.toyota.es",
            IsDeleted = false,
            CreatedAt = FechaCreacion,
            UpdatedAt = (DateTime?)null,
            DeletedAt = (DateTime?)null
        });
    }

    [Test]
    public void Constructor_ConCadenasVacias_ConservaLasCadenasVacias()
    {
        // Arrange & Act
        var marca = new Marca(0, string.Empty, string.Empty, string.Empty, false, default, null, null);

        // Assert
        marca.Should().BeEquivalentTo(new
        {
            Id = 0L,
            Nombre = string.Empty,
            Pais = string.Empty,
            Web = string.Empty
        });
    }

    #endregion

    #region Igualdad estructural

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
    public void Igualdad_ConDistintoIsDeleted_NoSonIguales()
    {
        // Arrange
        var marca = CrearMarca();
        var otraMarca = CrearMarca() with { IsDeleted = true };

        // Act & Assert
        marca.Should().NotBe(otraMarca);
    }

    [Test]
    public void Igualdad_ConDistintaFechaDeCreacion_NoSonIguales()
    {
        // Arrange
        var marca = CrearMarca();
        var otraMarca = CrearMarca() with { CreatedAt = FechaCreacion.AddDays(1) };

        // Act & Assert
        marca.Should().NotBe(otraMarca);
    }

    [Test]
    public void Igualdad_ConDistintaFechaDeBorrado_NoSonIguales()
    {
        // Arrange
        var marca = CrearMarca();
        var otraMarca = CrearMarca() with { DeletedAt = FechaCreacion.AddDays(1) };

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

    #endregion

    #region With, deconstrucción y ToString

    [Test]
    public void With_DevuelveNuevaInstanciaConLosCambios()
    {
        // Arrange
        var original = CrearMarca();
        var fechaBaja = new DateTime(2025, 5, 1);

        // Act
        var modificada = original with { Nombre = "Honda", IsDeleted = true, DeletedAt = fechaBaja };

        // Assert
        modificada.Should().BeEquivalentTo(new
        {
            Id = 1L,
            Nombre = "Honda",
            Pais = "Japón",
            Web = "https://www.toyota.es",
            IsDeleted = true,
            CreatedAt = FechaCreacion,
            DeletedAt = (DateTime?)fechaBaja
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
    public void Deconstruccion_DevuelveLosValoresEnElOrdenDeclarado()
    {
        // Arrange
        var marca = CrearMarca();

        // Act
        var (id, nombre, pais, web, isDeleted, createdAt, updatedAt, deletedAt) = marca;
        var descompuesta = (
            Id: id,
            Nombre: nombre,
            Pais: pais,
            Web: web,
            IsDeleted: isDeleted,
            CreatedAt: createdAt,
            UpdatedAt: updatedAt,
            DeletedAt: deletedAt);

        // Assert
        descompuesta.Should().Be((
            Id: 1L,
            Nombre: "Toyota",
            Pais: "Japón",
            Web: "https://www.toyota.es",
            IsDeleted: false,
            CreatedAt: FechaCreacion,
            UpdatedAt: (DateTime?)null,
            DeletedAt: (DateTime?)null));
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

    #endregion
}
