using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Dto.Catalogo_Vehiculos.Vehiculo;

[TestFixture]
[Category("Unit")]
public class CreateVehiculoDtoTests {
    [Test]
    public void Constructor_SinDatos_InicializaValoresPorDefecto() {
        // Arrange
        // No se necesitan datos previos.

        // Act
        var dto = new CreateVehiculoDto();

        // Assert
        dto.Should().BeEquivalentTo(new {
            Foto = "", Matricula = "", Marca = "", Modelo = "", Acriss = "",
            Color = "", PrecioBaseDia = 0m,
            Plazas = 0, MaleteroLitros = 0, AutonomiaKm = 0, Kilometraje = 0,
            FechaUltimaRevision = "", Concesionario = 0L
        });
    }

    [Test]
    public void Propiedades_AsignarDatos_ConservaValores() {
        // Arrange
        var dto = new CreateVehiculoDto();

        // Act
        dto.Foto = "foto-001";
        dto.Matricula = "1234BCD";
        dto.Marca = "Toyota";
        dto.Modelo = "Corolla";
        dto.Acriss = "ILAH";
        dto.Color = "Azul";
        dto.PrecioBaseDia = 45.50m;
        dto.Plazas = 5;
        dto.MaleteroLitros = 361;
        dto.AutonomiaKm = 700;
        dto.Kilometraje = 12000;
        dto.FechaUltimaRevision = "2026-11-01";
        dto.Concesionario = 10L;

        // Assert
        dto.Should().BeEquivalentTo(new {
            Foto = "foto-001", Matricula = "1234BCD", Marca = "Toyota", Modelo = "Corolla",
            Acriss = "ILAH",
            Color = "Azul", PrecioBaseDia = 45.50m, Plazas = 5,
            MaleteroLitros = 361, AutonomiaKm = 700, Kilometraje = 12000,
            FechaUltimaRevision = "2026-11-01", Concesionario = 10L
        });
    }

    [Test]
    public void With_CambiarPrecioYFoto_NoModificaElOriginal() {
        // Arrange
        var original = new CreateVehiculoDto {
            Matricula = "1234BCD", PrecioBaseDia = 45.50m, Foto = "foto-001"
        };

        // Act
        var modificado = original with { PrecioBaseDia = 60m, Foto = "foto-002" };

        // Assert
        original.PrecioBaseDia.Should().Be(45.50m);
        original.Foto.Should().Be("foto-001");
        modificado.PrecioBaseDia.Should().Be(60m);
        modificado.Foto.Should().Be("foto-002");
        modificado.Matricula.Should().Be(original.Matricula);
        modificado.Should().NotBeSameAs(original);
    }
}
