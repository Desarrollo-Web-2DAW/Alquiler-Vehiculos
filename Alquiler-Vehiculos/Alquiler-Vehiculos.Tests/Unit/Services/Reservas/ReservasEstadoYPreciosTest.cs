using Alquiler_Vehiculos.Enums;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Services.Reserva;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Reservas;

/// <summary>
/// Pruebas unitarias de la máquina de estados (RF-31..33, CU-21, RF-29)
/// y calculadora de precios/reembolsos (RF-13, RF-25, CU-09, CU-13, CU-24, CU-26).
/// </summary>
[TestFixture]
[Category("Unit")]
public class ReservaEstadoYPrecioTests {
    [TestCase(EstadoReserva.Pendiente, EstadoReserva.Confirmada)]
    [TestCase(EstadoReserva.Pendiente, EstadoReserva.Caducada)]
    [TestCase(EstadoReserva.Pendiente, EstadoReserva.Cancelada)]
    [TestCase(EstadoReserva.Confirmada, EstadoReserva.EnCurso)]
    [TestCase(EstadoReserva.Confirmada, EstadoReserva.Cancelada)]
    [TestCase(EstadoReserva.Confirmada, EstadoReserva.NoPresentado)]
    [TestCase(EstadoReserva.EnCurso, EstadoReserva.Finalizada)]
    public void ValidarTransicion_TransicionesPermitidas_DevuelveExito(EstadoReserva origen, EstadoReserva destino) {
        ReservaEstadoServices.ValidarTransicion(origen, destino).IsSuccess.Should().BeTrue();
    }

    [TestCase(EstadoReserva.Cancelada, EstadoReserva.EnCurso)]   // Ejemplo RF-32
    [TestCase(EstadoReserva.Pendiente, EstadoReserva.EnCurso)]   // Salto prohibido sin pago
    [TestCase(EstadoReserva.Pendiente, EstadoReserva.Finalizada)]
    [TestCase(EstadoReserva.Finalizada, EstadoReserva.Cancelada)]
    [TestCase(EstadoReserva.Caducada, EstadoReserva.Confirmada)]
    public void ValidarTransicion_Prohibidas_Devuelve409Conflict(EstadoReserva origen, EstadoReserva destino) {
        var resultado = ReservaEstadoServices.ValidarTransicion(origen, destino);
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ConflictError>();
    }

    [Test]
    public void ValidarModificable_EnCursoOFinalizada_Devuelve409() {
        ReservaEstadoServices.ValidarModificable(EstadoReserva.Pendiente).IsSuccess.Should().BeTrue();
        ReservaEstadoServices.ValidarModificable(EstadoReserva.Confirmada).IsSuccess.Should().BeTrue();
        ReservaEstadoServices.ValidarModificable(EstadoReserva.EnCurso).Error.Should().BeOfType<ConflictError>();
        ReservaEstadoServices.ValidarModificable(EstadoReserva.Finalizada).Error.Should().BeOfType<ConflictError>();
    }

    [Test]
    public void Calcular_MenorDe18EnRecogida_Devuelve400ValidationError() {
        var nacimiento = new DateOnly(2009, 12, 13); // cumple 18 un día después
        var recogida = new DateTime(2026, 12, 12, 10, 0, 0, DateTimeKind.Utc);

        var resultado = ReservaPrecioCalculator.Calcular(
            nacimiento, recogida, recogida.AddDays(3),
            "Renault Megane", 50m, "Básica", 0m);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>();
    }

    [Test]
    public void Calcular_Cumple18ElDiaDeRecogida_EsValidoYAplicaRecargo25PorCiento() {
        // CU-26: cumple 18 justo el día de recogida -> válido, y como < 25 lleva recargo 25 %
        var nacimiento = new DateOnly(2008, 12, 12);
        var recogida = new DateTime(2026, 12, 12, 10, 0, 0, DateTimeKind.Utc);
        var devolucion = new DateTime(2026, 12, 15, 10, 0, 0, DateTimeKind.Utc); // 3 días

        var resultado = ReservaPrecioCalculator.Calcular(
            nacimiento, recogida, devolucion,
            "Renault Megane", 50m, "Todo Riesgo", 15m,
            [new ExtraSeleccionado("Silla infantil", 5m, 2)]);

        resultado.IsSuccess.Should().BeTrue();
        var p = resultado.Value;
        // Vehículo: 3 * 50 = 150 | Póliza: 3 * 15 = 45 | Extra: 3 * 2 * 5 = 30 | Recargo 25% de 150 = 37.50 -> Total = 262.50
        p.PrecioTotal.Should().Be(262.50m);
        p.PrecioTotal.Should().Be(p.Lineas.Sum(l => l.Subtotal), "RF-25: PrecioTotal debe igualar la suma exacta de subtotales");
    }

    [Test]
    public void Calcular_DevolucionTardia2Dias_Suma2DiasALaLineaVehiculo() {
        // CU-24: reserva de 3 días + 2 días de retraso = 5 días en línea de vehículo
        var nacimiento = new DateOnly(1990, 1, 1);
        var recogida = new DateTime(2026, 12, 12, 10, 0, 0, DateTimeKind.Utc);
        var devolucion = new DateTime(2026, 12, 15, 10, 0, 0, DateTimeKind.Utc);

        var resultado = ReservaPrecioCalculator.Calcular(
            nacimiento, recogida, devolucion,
            "Ford Transit", 60m, "Básica", 0m, diasRetraso: 2);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Dias.Should().Be(5);
        resultado.Value.PrecioTotal.Should().Be(300m);
    }

    [Test]
    public void CalcularReembolsoNoPresentado_DescuentaUnDiaDeAlquiler() {
        // CU-13: pagó 200 € con tarifa de 50 €/día -> reembolso automático de 150 €
        ReservaPrecioCalculator.CalcularReembolsoNoPresentado(200m, 50m).Should().Be(150m);
    }
}