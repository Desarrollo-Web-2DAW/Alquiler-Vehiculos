using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Query;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Query;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators;

/// <summary>Casos compartidos para validar la búsqueda y su rechazo antes de consultar datos.</summary>
public static class QueryVehiculoCasos {
    public static QueryRequestVehiculoDto Clasificacion(string campo, string? valor) => campo switch {
        "Categoria" => new() { Categoria = valor },
        "Estado" => new() { Estado = valor },
        "Combustible" => new() { Combustible = valor },
        "Transmision" => new() { Transmision = valor },
        _ => throw new ArgumentException("Campo de prueba no admitido", nameof(campo))
    };

    public static IEnumerable<TestCaseData> Invalidos() {
        foreach (var campo in new[] { "Categoria", "Estado", "Combustible", "Transmision" })
            foreach (var valor in new[] { "0", "-1", "999", "NoExiste", " " })
                yield return new TestCaseData(Clasificacion(campo, valor), campo)
                    .SetName($"{{m}}_{campo}_{valor}");

        yield return new TestCaseData(Clasificacion("Categoria", "Sedan, Suv"), "Categoria");
        yield return new TestCaseData(Clasificacion("Estado", "Disponible, Reservado"), "Estado");
        yield return new TestCaseData(Clasificacion("Combustible", "Gasolina, Diesel"), "Combustible");
        yield return new TestCaseData(Clasificacion("Transmision", "Manual, Automatica"), "Transmision");

        yield return new TestCaseData(new QueryRequestVehiculoDto { PlazasMinimas = 0 }, "PlazasMinimas");
        yield return new TestCaseData(new QueryRequestVehiculoDto { PlazasMinimas = -1 }, "PlazasMinimas");
        yield return new TestCaseData(new QueryRequestVehiculoDto { MaleteroLitrosMinimos = -1 }, "MaleteroLitrosMinimos");
        yield return new TestCaseData(new QueryRequestVehiculoDto { AutonomiaKmMinima = 0 }, "AutonomiaKmMinima");
        yield return new TestCaseData(new QueryRequestVehiculoDto { AutonomiaKmMinima = -1 }, "AutonomiaKmMinima");

        // Fechas fijas muy alejadas del reloj actual: sin esperas ni límites de milisegundos.
        var recogida = new DateTime(2090, 6, 10, 10, 0, 0, DateTimeKind.Utc);
        yield return new TestCaseData(new QueryRequestVehiculoDto { FechaRecogida = recogida }, "FechaDevolucion");
        yield return new TestCaseData(new QueryRequestVehiculoDto { FechaDevolucion = recogida }, "FechaRecogida");
        yield return new TestCaseData(new QueryRequestVehiculoDto {
            FechaRecogida = new DateTime(2000, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            FechaDevolucion = new DateTime(2000, 1, 2, 10, 0, 0, DateTimeKind.Utc)
        }, "FechaRecogida");
        foreach (var dias in new[] { -1, 0, 31 })
            yield return new TestCaseData(new QueryRequestVehiculoDto {
                FechaRecogida = recogida, FechaDevolucion = recogida.AddDays(dias)
            }, "FechaDevolucion");
        yield return new TestCaseData(new QueryRequestVehiculoDto {
            FechaRecogida = recogida, FechaDevolucion = recogida.AddHours(2)
        }, "FechaDevolucion");
    }

    public static IEnumerable<TestCaseData> Validos() {
        yield return new TestCaseData(new QueryRequestVehiculoDto());
        yield return new TestCaseData(new QueryRequestVehiculoDto {
            Marca = "REn", Modelo = "meg", PlazasMinimas = 1,
            MaleteroLitrosMinimos = 0, AutonomiaKmMinima = 1
        });
        foreach (var campo in new[] { "Categoria", "Estado", "Combustible", "Transmision" }) {
            var nombres = campo switch {
                "Categoria" => Enum.GetNames<TipoCarroceria>(),
                "Estado" => Enum.GetNames<EstadoVehiculo>(),
                "Combustible" => Enum.GetNames<TipoCombustible>(),
                _ => Enum.GetNames<TipoTransmision>()
            };
            foreach (var nombre in nombres)
                yield return new TestCaseData(Clasificacion(campo, nombre.ToLowerInvariant()))
                    .SetName($"{{m}}_{campo}_{nombre}");
            yield return new TestCaseData(Clasificacion(campo, ""));
        }
        var recogida = new DateTime(2090, 6, 10, 18, 0, 0, DateTimeKind.Utc);
        foreach (var dias in new[] { 1, 3, 30 })
            yield return new TestCaseData(new QueryRequestVehiculoDto {
                FechaRecogida = recogida, FechaDevolucion = recogida.AddDays(dias)
            });
        // RF-19 cuenta fechas: las horas se comprobarán contra el horario de oficina, no aquí.
        yield return new TestCaseData(new QueryRequestVehiculoDto {
            FechaRecogida = recogida, FechaDevolucion = recogida.Date.AddDays(1).AddHours(9)
        });
    }
}

[TestFixture]
[Category("Unit")]
public class QueryRequestVehiculoValidatorTests {
    [TestCaseSource(typeof(QueryVehiculoCasos), nameof(QueryVehiculoCasos.Validos))]
    public async Task Validate_FiltrosValidos_Acepta(QueryRequestVehiculoDto request) {
        // Arrange
        var validator = new QueryRequestVehiculoValidator();

        // Act
        var resultado = await validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [TestCaseSource(typeof(QueryVehiculoCasos), nameof(QueryVehiculoCasos.Invalidos))]
    public async Task Validate_FiltroInvalido_IndicaCampo(QueryRequestVehiculoDto request, string campo) {
        // Arrange
        var validator = new QueryRequestVehiculoValidator();

        // Act
        var resultado = await validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == campo && !string.IsNullOrEmpty(e.ErrorMessage));
    }

    [Test]
    public async Task Validate_VariosErrores_DevuelveTodosSinLanzarExcepciones() {
        // Arrange
        var validator = new QueryRequestVehiculoValidator();
        var request = new QueryRequestVehiculoDto { Categoria = "Inexistente", PlazasMinimas = -1, AutonomiaKmMinima = 0 };

        // Act
        var resultado = await validator.ValidateAsync(request);

        // Assert
        resultado.Errors.Select(e => e.PropertyName).Should().BeEquivalentTo(new[] { "Categoria", "PlazasMinimas", "AutonomiaKmMinima" });
    }
}
