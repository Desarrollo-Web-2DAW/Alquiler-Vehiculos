using FluentAssertions;
using OficinaEntity = Alquiler_Vehiculos.Models.Oficina.Oficina;

namespace Alquiler_Vehiculos.Tests.Unit.Oficina;

/// <summary>
/// Pruebas unitarias de reglas de horario semanal en <see cref="OficinaEntity"/> (RF-11, CU-22).
/// </summary>
[TestFixture]
[Category("Unit")]
public class OficinaModelTests {
    private OficinaEntity _oficina = null!;

    [SetUp]
    public void SetUp() {
        _oficina = new OficinaEntity {
            DiaInicio = DayOfWeek.Monday,
            DiaFin = DayOfWeek.Friday,
            HoraApertura = new TimeOnly(9, 0),
            HoraCierre = new TimeOnly(20, 0)
        };
    }

    [Test]
    public void EstaAbierta_MiercolesDentroDeHorario_DevuelveTrue() {
        // Arrange: 2026-11-25 es miércoles
        var fecha = new DateTime(2026, 11, 25, 10, 30, 0);

        // Act
        var abierta = _oficina.EstaAbierta(fecha);

        // Assert
        abierta.Should().BeTrue();
    }

    [Test]
    public void EstaAbierta_MiercolesAntesDeApertura_DevuelveFalse() {
        // Arrange
        var fecha = new DateTime(2026, 11, 25, 8, 59, 0);

        // Act
        var abierta = _oficina.EstaAbierta(fecha);

        // Assert
        abierta.Should().BeFalse();
    }

    [Test]
    public void EstaAbierta_MiercolesDespuesDeCierre_DevuelveFalse() {
        // Arrange
        var fecha = new DateTime(2026, 11, 25, 20, 1, 0);

        // Act
        var abierta = _oficina.EstaAbierta(fecha);

        // Assert
        abierta.Should().BeFalse();
    }

    [Test]
    public void EstaAbierta_SabadoCerrado_DevuelveFalse() {
        // Arrange: 2026-11-28 es sábado
        var fecha = new DateTime(2026, 11, 28, 12, 0, 0);

        // Act
        var abierta = _oficina.EstaAbierta(fecha);

        // Assert
        abierta.Should().BeFalse();
    }

    [Test]
    public void EstaAbierta_LunesADomingo_DomingoDevuelveTrue() {
        // Arrange: 2026-11-29 es domingo
        _oficina.DiaFin = DayOfWeek.Sunday;
        var fecha = new DateTime(2026, 11, 29, 12, 0, 0);

        // Act
        var abierta = _oficina.EstaAbierta(fecha);

        // Assert
        abierta.Should().BeTrue();
    }

    [Test]
    public void HorarioFormateado_LunesAViernes_DevuelveTextoLegibleEnCastellano() {
        // Act & Assert
        _oficina.HorarioFormateado.Should().Be("Lunes a Viernes de 09:00 a 20:00");
    }
}
