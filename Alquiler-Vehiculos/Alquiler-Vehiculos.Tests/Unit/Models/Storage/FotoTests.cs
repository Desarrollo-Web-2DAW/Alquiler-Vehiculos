using Alquiler_Vehiculos.Models.Storage;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Models.Storage;

[TestFixture]
[Category("Unit")]
public class FotoTests {
    [Test]
    public void Constructor_SinValores_InicializaFotoActivaSinModificaciones() {
        // Arrange
        var antes = DateTime.UtcNow;

        // Act
        var foto = new Foto();

        // Assert
        foto.Nombre.Should().BeEmpty();
        foto.Ruta.Should().BeEmpty();
        foto.Extension.Should().BeEmpty();
        foto.Tamano.Should().Be(0);
        foto.Activo.Should().BeTrue();
        foto.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
        foto.CreatedAt.Should().BeOnOrAfter(antes).And.BeOnOrBefore(DateTime.UtcNow);
        foto.UpdatedAt.Should().BeNull();
        foto.DeletedAt.Should().BeNull();
    }

    [Test]
    public void Propiedades_ValoresAsignados_ConservaDatos() {
        // Arrange
        var fecha = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var foto = new Foto {
            Nombre = "coche.png", Ruta = "/uploads/vehiculos/coche.png", Extension = ".png",
            Tamano = 1024, Activo = false, CreatedAt = fecha,
            UpdatedAt = fecha.AddDays(1), DeletedAt = fecha.AddDays(2)
        };

        // Assert
        foto.Nombre.Should().Be("coche.png");
        foto.Ruta.Should().Be("/uploads/vehiculos/coche.png");
        foto.Extension.Should().Be(".png");
        foto.Tamano.Should().Be(1024);
        foto.Activo.Should().BeFalse();
        foto.CreatedAt.Should().Be(fecha);
        foto.UpdatedAt.Should().Be(fecha.AddDays(1));
        foto.DeletedAt.Should().Be(fecha.AddDays(2));
    }
}
