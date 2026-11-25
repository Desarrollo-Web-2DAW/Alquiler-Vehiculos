using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

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
            Foto = (IFormFile?)null, Matricula = "", Marca = "", Modelo = "", Acriss = "",
            Color = "", PrecioBaseDia = 0m,
            Plazas = 0, MaleteroLitros = 0, AutonomiaKm = 0, Kilometraje = 0,
            FechaUltimaRevision = "", Concesionario = 0L
        });
    }

    [Test]
    public void Propiedades_AsignarDatos_ConservaValores() {
        // Arrange
        var dto = new CreateVehiculoDto();
        using var contenido = new MemoryStream([1]);
        var foto = new FormFile(contenido, 0, contenido.Length, "foto", "foto.jpg");

        // Act
        dto.Foto = foto;
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
        dto.Foto.Should().BeSameAs(foto);
        dto.Matricula.Should().Be("1234BCD");
        dto.Marca.Should().Be("Toyota");
        dto.Modelo.Should().Be("Corolla");
        dto.Acriss.Should().Be("ILAH");
        dto.Color.Should().Be("Azul");
        dto.PrecioBaseDia.Should().Be(45.50m);
        dto.Plazas.Should().Be(5);
        dto.MaleteroLitros.Should().Be(361);
        dto.AutonomiaKm.Should().Be(700);
        dto.Kilometraje.Should().Be(12000);
        dto.FechaUltimaRevision.Should().Be("2026-11-01");
        dto.Concesionario.Should().Be(10L);
    }

    [Test]
    public void With_CambiarPrecioYFoto_NoModificaElOriginal() {
        // Arrange
        using var contenido = new MemoryStream([1, 2]);
        var fotoOriginal = new FormFile(contenido, 0, 1, "foto", "foto-001.jpg");
        using var contenidoModificado = new MemoryStream([3, 4]);
        var fotoNueva = new FormFile(contenidoModificado, 0, 1, "foto", "foto-002.jpg");
        var original = new CreateVehiculoDto { Matricula = "1234BCD", PrecioBaseDia = 45.50m, Foto = fotoOriginal };

        // Act
        var modificado = original with { PrecioBaseDia = 60m, Foto = fotoNueva };

        // Assert
        original.PrecioBaseDia.Should().Be(45.50m);
        original.Foto.Should().BeSameAs(fotoOriginal);
        modificado.PrecioBaseDia.Should().Be(60m);
        modificado.Foto.Should().BeSameAs(fotoNueva);
        modificado.Matricula.Should().Be(original.Matricula);
        modificado.Should().NotBeSameAs(original);
    }
}
