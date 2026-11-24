using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Trabajadores;

[TestFixture]
[Category("Unit")]
public class TrabajadorTests {
    [Test]
    public void Constructor_AlInstanciar_InicializaValoresPorDefectoCorrectos() {
        // Arrange & Act
        var trabajador = new Trabajador();

        // Assert
        trabajador.Id.Should().NotBeEmpty();
        trabajador.Tipo.Should().Be(TipoTrabajador.Operaciones);
        trabajador.Activo.Should().BeTrue();
        trabajador.IsDeleted.Should().BeFalse();
        trabajador.DeletedAt.Should().BeNull();
        trabajador.Nombre.Should().BeEmpty();
        trabajador.Apellidos.Should().BeEmpty();
        trabajador.Email.Should().BeEmpty();
        trabajador.Dni.Should().BeEmpty();
    }

    [Test]
    public void Constructor_DosInstancias_GeneranIdsDistintos() {
        // Arrange & Act
        var primero = new Trabajador();
        var segundo = new Trabajador();

        // Assert
        primero.Id.Should().NotBe(segundo.Id);
    }

    [Test]
    public void BajaLogica_AlDesactivar_PermiteMarcarInactivoYDeletedAt() {
        // Arrange
        var trabajador = new Trabajador();
        var fechaBaja = new DateTime(2026, 11, 22, 10, 0, 0, DateTimeKind.Utc);

        // Act
        trabajador.Activo = false;
        trabajador.IsDeleted = true;
        trabajador.DeletedAt = fechaBaja;

        // Assert
        trabajador.Activo.Should().BeFalse();
        trabajador.IsDeleted.Should().BeTrue();
        trabajador.DeletedAt.Should().Be(fechaBaja);
    }
}