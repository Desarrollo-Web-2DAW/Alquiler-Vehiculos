using Alquiler_Vehiculos.Data.Seed.Vehiculo;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Data.Seed.Vehiculo;

[TestFixture]
[Category("Unit")]
public class ConcesionarioSeederTests {
    private static readonly DateTime Fecha = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void BuildConcesionarios_FechaFija_DevuelveCincoConDatosDeContacto() {
        // Arrange
        string[] nombres = ["Motor Leganés", "Automóviles Madrid Centro", "Motor Getafe", "Vehículos Alcorcón", "Motor Fuenlabrada"];

        // Act
        var concesionarios = ConcesionarioSeeder.BuildConcesionarios(Fecha);

        // Assert
        concesionarios.Select(c => c.Nombre).Should().Equal(nombres);
        concesionarios.Select(c => c.Nombre).Should().OnlyHaveUniqueItems();
        concesionarios.Select(c => c.Email).Should().OnlyHaveUniqueItems();
        foreach (var concesionario in concesionarios) {
            concesionario.Ciudad.Should().NotBeNullOrWhiteSpace();
            concesionario.Telefono.Should().MatchRegex("^[0-9]{9}$");
            concesionario.Email.Should().MatchRegex("^[^@]+@[^@]+\\.example$");
        }
    }

    [Test]
    public void BuildConcesionarios_SinPersistir_DejaIdentificadoresParaPostgreSql() {
        // Arrange
        var fecha = Fecha;

        // Act
        var concesionarios = ConcesionarioSeeder.BuildConcesionarios(fecha);

        // Assert
        concesionarios.Should().OnlyContain(c => c.Id == 0);
    }

    [Test]
    public void BuildConcesionarios_FechaFija_ConservaUnInactivoConAuditoria() {
        // Arrange
        var fecha = Fecha;

        // Act
        var concesionarios = ConcesionarioSeeder.BuildConcesionarios(fecha);

        // Assert
        concesionarios.Where(c => !c.IsDeleted).Should().HaveCount(4)
            .And.OnlyContain(c => c.CreatedAt == fecha && c.UpdatedAt == null && c.DeletedAt == null);
        var inactivo = concesionarios.Should().ContainSingle(c => c.IsDeleted).Which;
        inactivo.Nombre.Should().Be("Motor Fuenlabrada");
        inactivo.CreatedAt.Should().Be(fecha.AddMonths(-6));
        inactivo.UpdatedAt.Should().Be(fecha);
        inactivo.DeletedAt.Should().Be(fecha);
    }
}
