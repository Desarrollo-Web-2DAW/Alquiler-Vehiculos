using Alquiler_Vehiculos.Models.Vehiculo;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Models;

/// <summary>Pruebas de construcción, colección de modelos, igualdad y auditoría de una marca.</summary>
[TestFixture]
[Category("Unit")]
public class MarcaTests {
    private static readonly DateTime Fecha = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
    private static Marca CrearMarca() => new(1, "Toyota", "Japón", "https://www.toyota.es",
        new List<string> { "Corolla", "Yaris" }, false, Fecha, null, null);

    [Test]
    public void Constructor_ConTodosLosValores_AsignaCadaPropiedad() {
        // Arrange
        var modelos = new List<string> { "Corolla", "Yaris" };

        // Act
        var marca = new Marca(1, "Toyota", "Japón", "https://www.toyota.es", modelos,
            false, Fecha, null, null);

        // Assert
        marca.Should().BeEquivalentTo(new {
            Id = 1L, Nombre = "Toyota", Pais = "Japón", Web = "https://www.toyota.es",
            ModelosDisponibles = new[] { "Corolla", "Yaris" }, IsDeleted = false,
            CreatedAt = Fecha, UpdatedAt = (DateTime?)null, DeletedAt = (DateTime?)null
        });
        marca.ModelosDisponibles.Should().BeSameAs(modelos);
    }

    [Test]
    public void Constructor_ConCadenasYColeccionVacias_ConservaLosValores() {
        // Arrange
        var modelos = new List<string>();

        // Act
        var marca = new Marca(0, "", "", "", modelos, false, Fecha, null, null);

        // Assert
        marca.Id.Should().Be(0L);
        marca.Nombre.Should().BeEmpty();
        marca.Pais.Should().BeEmpty();
        marca.Web.Should().BeEmpty();
        marca.ModelosDisponibles.Should().BeEmpty();
    }

    [Test]
    public void Constructor_ConAuditoria_ConservaLosValores() {
        // Arrange
        var modelos = new List<string>();

        // Act
        var marca = new Marca(1, "Toyota", "Japón", "https://www.toyota.es", modelos,
            true, Fecha, Fecha.AddDays(1), Fecha.AddDays(2));

        // Assert
        marca.IsDeleted.Should().BeTrue();
        marca.CreatedAt.Should().Be(Fecha);
        marca.UpdatedAt.Should().Be(Fecha.AddDays(1));
        marca.DeletedAt.Should().Be(Fecha.AddDays(2));
    }

    [Test]
    public void Igualdad_MismosValoresYMismaColeccion_SonIguales() {
        // Arrange
        var marca = CrearMarca();
        var otra = new Marca(marca.Id, marca.Nombre, marca.Pais, marca.Web,
            marca.ModelosDisponibles, marca.IsDeleted, marca.CreatedAt, marca.UpdatedAt, marca.DeletedAt);

        // Act
        var iguales = marca.Equals(otra);

        // Assert
        iguales.Should().BeTrue();
        otra.Should().NotBeSameAs(marca);
        otra.GetHashCode().Should().Be(marca.GetHashCode());
    }

    [TestCase("Id")]
    [TestCase("Nombre")]
    [TestCase("Pais")]
    [TestCase("Web")]
    [TestCase("ModelosDisponibles")]
    public void Igualdad_ConPropiedadDistinta_NoSonIguales(string propiedad) {
        // Arrange
        var marca = CrearMarca();
        var otra = propiedad switch {
            "Id" => marca with { Id = 2 },
            "Nombre" => marca with { Nombre = "Honda" },
            "Pais" => marca with { Pais = "Alemania" },
            "Web" => marca with { Web = "https://www.honda.es" },
            _ => marca with { ModelosDisponibles = new List<string> { "Corolla", "Yaris" } }
        };

        // Act
        var iguales = marca.Equals(otra);

        // Assert
        iguales.Should().BeFalse();
    }

    [TestCase(null)]
    [TestCase("Toyota")]
    public void Equals_ConNullUOtroTipo_DevuelveFalse(object? otro) {
        // Arrange
        var marca = CrearMarca();

        // Act
        var iguales = marca.Equals(otro);

        // Assert
        iguales.Should().BeFalse();
    }

    [Test]
    public void With_CambiarNombre_ConservaElOriginalYComparteLaColeccion() {
        // Arrange
        var original = CrearMarca();

        // Act
        var modificada = original with { Nombre = "Honda" };

        // Assert
        original.Nombre.Should().Be("Toyota");
        modificada.Should().BeEquivalentTo(new {
            Id = 1L, Nombre = "Honda", Pais = "Japón", Web = "https://www.toyota.es"
        });
        modificada.Should().NotBeSameAs(original);
        modificada.ModelosDisponibles.Should().BeSameAs(original.ModelosDisponibles);
    }

    [Test]
    public void ModelosDisponibles_AgregarModelo_ConservaElNuevoElemento() {
        // Arrange
        var marca = CrearMarca();

        // Act
        marca.ModelosDisponibles.Add("Prius");

        // Assert
        marca.ModelosDisponibles.Should().Equal("Corolla", "Yaris", "Prius");
    }

    [Test]
    public void ToString_ContieneLosDatosDeLaMarca() {
        // Arrange
        var marca = CrearMarca();

        // Act
        var texto = marca.ToString();

        // Assert
        texto.Should().ContainAll("1", "Toyota", "Japón", "https://www.toyota.es");
    }
}
