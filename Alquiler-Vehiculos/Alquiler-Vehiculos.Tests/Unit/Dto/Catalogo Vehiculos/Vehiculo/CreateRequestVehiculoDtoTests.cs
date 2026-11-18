using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Dto.Catalogo_Vehiculos.Vehiculo;

[TestFixture]
public class CreateRequestVehiculoDtoTests {
    [Test]
    public void Constructor_SinDatos_InicializaValoresPorDefecto() {
        // Arrange & Act
        var dto = new CreateRequestVehiculoDto();

        // Assert
        using (Assert.EnterMultipleScope()) {
            Assert.That(dto.Matricula, Is.Empty);
            Assert.That(dto.Acriss, Is.Empty);
            Assert.That(dto.Categoria, Is.Empty);
            Assert.That(dto.PrecioBaseDia, Is.Zero);
            Assert.That(dto.Color, Is.Empty);
            Assert.That(dto.Estado, Is.Empty);
            Assert.That(dto.Combustible, Is.Empty);
            Assert.That(dto.Transmision, Is.Empty);
            Assert.That(dto.Plazas, Is.Zero);
            Assert.That(dto.MaleteroLitros, Is.Zero);
            Assert.That(dto.AutonomiaKm, Is.Zero);
            Assert.That(dto.Kilometraje, Is.Zero);
            Assert.That(dto.FechaUltimaRevision, Is.Empty);
            Assert.That(dto.IsActivo, Is.False);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Propiedades_AsignarDatos_ConservaValores(bool isActivo) {
        // Arrange
        var dto = new CreateRequestVehiculoDto();

        // Act
        dto.Matricula = "1234BCD";
        dto.Acriss = "CDMR";
        dto.Categoria = "Compacto";
        dto.PrecioBaseDia = 45.50;
        dto.Color = "Azul";
        dto.Estado = "Disponible";
        dto.Combustible = "Gasolina";
        dto.Transmision = "Manual";
        dto.Plazas = 5;
        dto.MaleteroLitros = 361;
        dto.AutonomiaKm = 700;
        dto.Kilometraje = 12000;
        dto.FechaUltimaRevision = "2026-11-01";
        dto.IsActivo = isActivo;

        // Assert
        using (Assert.EnterMultipleScope()) {
            Assert.That(dto.Matricula, Is.EqualTo("1234BCD"));
            Assert.That(dto.Acriss, Is.EqualTo("CDMR"));
            Assert.That(dto.Categoria, Is.EqualTo("Compacto"));
            dto.PrecioBaseDia.Should().Be(45.50d);
            Assert.That(dto.Color, Is.EqualTo("Azul"));
            Assert.That(dto.Estado, Is.EqualTo("Disponible"));
            Assert.That(dto.Combustible, Is.EqualTo("Gasolina"));
            Assert.That(dto.Transmision, Is.EqualTo("Manual"));
            Assert.That(dto.Plazas, Is.EqualTo(5));
            Assert.That(dto.MaleteroLitros, Is.EqualTo(361));
            Assert.That(dto.AutonomiaKm, Is.EqualTo(700));
            Assert.That(dto.Kilometraje, Is.EqualTo(12000));
            Assert.That(dto.FechaUltimaRevision, Is.EqualTo("2026-11-01"));
            Assert.That(dto.IsActivo, Is.EqualTo(isActivo));
        }
    }

    [Test]
    public void With_CambiarPrecio_NoModificaElOriginal() {
        // Arrange
        var original = new CreateRequestVehiculoDto { Matricula = "1234BCD", PrecioBaseDia = 45.50d };

        // Act
        var modificado = original with { PrecioBaseDia = 60d };

        // Assert
        original.PrecioBaseDia.Should().Be(45.50d);
        modificado.PrecioBaseDia.Should().Be(60d);
        modificado.Matricula.Should().Be(original.Matricula);
        modificado.Should().NotBeSameAs(original);
    }
}
