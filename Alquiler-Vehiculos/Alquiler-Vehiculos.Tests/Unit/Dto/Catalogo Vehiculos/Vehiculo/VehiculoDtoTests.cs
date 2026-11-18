using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Dto.Catalogo_Vehiculos.Vehiculo;

[TestFixture]
public class VehiculoDtoTests {
    [Test]
    public void Constructor_SinDatos_InicializaValoresPorDefecto() {
        // Arrange & Act
        var dto = new VehiculoDto();

        // Assert
        using (Assert.EnterMultipleScope()) {
            Assert.That(dto.Matricula, Is.Empty);
            Assert.That(dto.Acriss, Is.Empty);
            Assert.That(dto.Categoria, Is.Empty);
            Assert.That(dto.PrecioBaseDia, Is.Zero);
            Assert.That(dto.Color, Is.Empty);
            Assert.That(dto.Combustible, Is.Empty);
            Assert.That(dto.Transmision, Is.Empty);
            Assert.That(dto.Plazas, Is.Zero);
            Assert.That(dto.MaleteroLitros, Is.Zero);
            Assert.That(dto.AutonomiaKm, Is.Zero);
            Assert.That(dto.Marca, Is.Empty);
            Assert.That(dto.Modelo, Is.Empty);
        }
    }

    [Test]
    public void Propiedades_AsignarDatos_ConservaValores() {
        // Arrange
        var dto = new VehiculoDto();

        // Act
        dto.Matricula = "1234BCD";
        dto.Acriss = "CDMR";
        dto.Categoria = "Compacto";
        dto.PrecioBaseDia = 45.50m;
        dto.Color = "Azul";
        dto.Combustible = "Gasolina";
        dto.Transmision = "Manual";
        dto.Plazas = 5;
        dto.MaleteroLitros = 361;
        dto.AutonomiaKm = 700;
        dto.Marca = "Toyota";
        dto.Modelo = "Corolla";

        // Assert
        using (Assert.EnterMultipleScope()) {
            Assert.That(dto.Matricula, Is.EqualTo("1234BCD"));
            Assert.That(dto.Acriss, Is.EqualTo("CDMR"));
            Assert.That(dto.Categoria, Is.EqualTo("Compacto"));
            Assert.That(dto.PrecioBaseDia, Is.EqualTo(45.50m));
            Assert.That(dto.Color, Is.EqualTo("Azul"));
            Assert.That(dto.Combustible, Is.EqualTo("Gasolina"));
            Assert.That(dto.Transmision, Is.EqualTo("Manual"));
            Assert.That(dto.Plazas, Is.EqualTo(5));
            Assert.That(dto.MaleteroLitros, Is.EqualTo(361));
            Assert.That(dto.AutonomiaKm, Is.EqualTo(700));
            Assert.That(dto.Marca, Is.EqualTo("Toyota"));
            Assert.That(dto.Modelo, Is.EqualTo("Corolla"));
        }
    }

    [Test]
    public void With_CambiarPrecio_NoModificaElOriginal() {
        // Arrange
        var original = new VehiculoDto { Matricula = "1234BCD", PrecioBaseDia = 45.50m };

        // Act
        var modificado = original with { PrecioBaseDia = 60m };

        // Assert
        original.PrecioBaseDia.Should().Be(45.50m);
        modificado.PrecioBaseDia.Should().Be(60m);
        modificado.Matricula.Should().Be(original.Matricula);
        modificado.Should().NotBeSameAs(original);
    }
}
