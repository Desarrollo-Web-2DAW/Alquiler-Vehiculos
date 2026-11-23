using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Query;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Alquiler_Vehiculos.Tests.Unit.Dto;

[TestFixture]
[Category("Unit")]
public class QueryVehiculoDtoTests {
    [Test]
    public void Request_SinFiltros_TodasLasPropiedadesSonNulas() {
        // Arrange
        var request = new QueryRequestVehiculoDto();

        // Act
        var valores = new object?[] { request.Marca, request.Modelo, request.Categoria, request.Estado,
            request.Combustible, request.Transmision, request.PlazasMinimas, request.MaleteroLitrosMinimos,
            request.AutonomiaKmMinima, request.FechaRecogida, request.FechaDevolucion };

        // Assert
        valores.Should().OnlyContain(v => v == null);
    }

    [Test]
    public void Response_ConRecuentos_ConservaLosDatosDelGrupo() {
        // Arrange
        var original = new QueryResponseVehiculoDto {
            Marca = "Renault", Modelo = "Megane", Categoria = "Hatchback", UnidadesTotales = 5, UnidadesLibres = 2
        };

        // Act
        var copia = original with { UnidadesLibres = 0 };

        // Assert
        copia.Should().BeEquivalentTo(new { original.Marca, original.Modelo, original.Categoria, UnidadesTotales = 5, UnidadesLibres = 0 });
        original.UnidadesLibres.Should().Be(2);
    }

    [Test]
    public void Response_PorDefecto_TextosVaciosYRecuentosCero() {
        // Arrange
        var response = new QueryResponseVehiculoDto();

        // Act
        var valores = (response.Marca, response.Modelo, response.Categoria, response.UnidadesTotales, response.UnidadesLibres);

        // Assert
        valores.Should().Be(("", "", "", 0, 0));
    }

    [Test]
    public void NotFoundVehiculoMatricula_ConMatricula_Devuelve404YMensajeCorrecto() {
        // Arrange
        const string matricula = "1234BCD";

        // Act
        var error = VehiculoError.NotFoundVehiculoMatricula(matricula);

        // Assert
        error.Should().BeOfType<NotFoundError>();
        error.Code.Should().Be("VEHICULO_NO_ENCONTRADO");
        error.Message.Should().Be("Vehículo con matrícula '1234BCD' no encontrado");
        error.ToHttpResult().Should().BeOfType<NotFoundObjectResult>();
    }
}
