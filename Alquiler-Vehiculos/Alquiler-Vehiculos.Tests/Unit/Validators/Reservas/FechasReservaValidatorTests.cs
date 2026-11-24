using Alquiler_Vehiculos.Validators.Reservas;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators;

/// <summary>
/// Pruebas unitarias de <see cref="FechasReservaValidator"/> (RF-19, RF-20, CU-25).
/// </summary>
[TestFixture]
[Category("Unit")]
public class FechasReservaValidatorTests {
    private static readonly DateTime Ahora = new(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc);
    private FechasReservaValidator _validator = null!;

    [SetUp]
    public void SetUp() {
        _validator = new FechasReservaValidator(new RelojFijo(new DateTimeOffset(Ahora)));
    }

    [Test]
    public async Task ValidateAsync_RangoDeTresDias_EsValidoYCalcula3Dias() {
        // Arrange (ejemplo del enunciado RF-19: del 12 al 15 = 3 días)
        var rango = new RangoFechasReserva(
            new DateTime(2026, 12, 12, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 15, 18, 0, 0, DateTimeKind.Utc));

        // Act
        var resultado = await _validator.ValidateAsync(rango);
        var dias = FechasReservaValidator.CalcularDias(rango.FechaRecogida, rango.FechaDevolucion);

        // Assert
        resultado.IsValid.Should().BeTrue();
        dias.Should().Be(3);
    }

    [Test]
    public async Task ValidateAsync_LimiteMinimoUnDia_EsValido() {
        // Arrange
        var rango = new RangoFechasReserva(Ahora.AddHours(1), Ahora.AddDays(1));

        // Act
        var resultado = await _validator.ValidateAsync(rango);

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ValidateAsync_LimiteMaximoTreintaDias_EsValido() {
        // Arrange
        var rango = new RangoFechasReserva(Ahora.AddDays(1), Ahora.AddDays(31));

        // Act
        var resultado = await _validator.ValidateAsync(rango);

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ValidateAsync_RecogidaEnElPasado_Falla() {
        // Arrange
        var rango = new RangoFechasReserva(Ahora.AddMinutes(-1), Ahora.AddDays(2));

        // Act
        var resultado = await _validator.ValidateAsync(rango);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(RangoFechasReserva.FechaRecogida));
    }

    [Test]
    public async Task ValidateAsync_MismoDiaDosHoras_FallaPorDuracionMinima() {
        // Arrange (CU-25: cliente pide coche para dos horas)
        var rango = new RangoFechasReserva(Ahora.AddHours(1), Ahora.AddHours(3));

        // Act
        var resultado = await _validator.ValidateAsync(rango);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "fechas");
    }

    [Test]
    public async Task ValidateAsync_DevolucionAnteriorARecogida_Falla() {
        // Arrange
        var rango = new RangoFechasReserva(Ahora.AddDays(5), Ahora.AddDays(3));

        // Act
        var resultado = await _validator.ValidateAsync(rango);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "fechas");
    }

    [Test]
    public async Task ValidateAsync_MasDeTreintaDias_FallaPorDuracionMaxima() {
        // Arrange (CU-25: alquiler de dos meses)
        var rango = new RangoFechasReserva(Ahora.AddDays(1), Ahora.AddDays(61));

        // Act
        var resultado = await _validator.ValidateAsync(rango);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "fechas");
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}