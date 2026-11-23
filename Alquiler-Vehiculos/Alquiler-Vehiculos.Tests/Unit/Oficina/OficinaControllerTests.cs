using Alquiler_Vehiculos.Controllers.Oficina;
using Alquiler_Vehiculos.Dto.Oficina;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Oficina;
using Alquiler_Vehiculos.Services.Oficina;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Oficina;

/// <summary>
/// Pruebas unitarias de <see cref="OficinaController"/> doblando <see cref="IOficinaService"/> con Moq.
/// </summary>
[TestFixture]
[Category("Unit")]
public class OficinaControllerTests {
    private Mock<IOficinaService> _serviceMock = null!;
    private OficinaController _controller = null!;

    [SetUp]
    public void SetUp() {
        _serviceMock = new Mock<IOficinaService>();
        _controller = new OficinaController(_serviceMock.Object, NullLogger<OficinaController>.Instance);
    }

    [Test]
    public async Task Obtener_OficinaExistente_Devuelve200ConDatos() {
        // Arrange
        var dto = CrearRespuesta();
        _serviceMock
            .Setup(s => s.ObtenerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<OficinaResponseDto, DomainError>(dto));

        // Act
        var result = await _controller.Obtener(CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(dto);
    }

    [Test]
    public async Task Obtener_OficinaNoConfigurada_Devuelve404NotFound() {
        // Arrange
        _serviceMock
            .Setup(s => s.ObtenerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<OficinaResponseDto, DomainError>(OficinaError.NoConfigurada()));

        // Act
        var result = await _controller.Obtener(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Actualizar_DatosValidos_Devuelve200ConOficinaActualizada() {
        // Arrange
        var input = new UpdateOficinaDto { Nombre = "Central" };
        var respuesta = CrearRespuesta();
        _serviceMock
            .Setup(s => s.ActualizarAsync(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<OficinaResponseDto, DomainError>(respuesta));

        // Act
        var result = await _controller.Actualizar(input, CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(respuesta);
    }

    [Test]
    public async Task Actualizar_HorarioInvalido_Devuelve400BadRequest() {
        // Arrange
        var input = new UpdateOficinaDto();
        _serviceMock
            .Setup(s => s.ActualizarAsync(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<OficinaResponseDto, DomainError>(
                OficinaError.ValidacionConCampos(new Dictionary<string, string[]> {
                    ["horario"] = ["La hora de cierre debe ser posterior a la hora de apertura"]
                })));

        // Act
        var result = await _controller.Actualizar(input, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task CambiarResponsable_TrabajadorMecanico_Devuelve422UnprocessableEntity() {
        // Arrange
        var dto = new CambiarResponsableDto { TrabajadorId = Guid.NewGuid() };
        _serviceMock
            .Setup(s => s.CambiarResponsableAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<OficinaResponseDto, DomainError>(
                OficinaError.ResponsableDebeSerOperaciones(dto.TrabajadorId)));

        // Act
        var result = await _controller.CambiarResponsable(dto, CancellationToken.None);

        // Assert
        var obj = result.Result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    [Test]
    public async Task CambiarResponsable_OperacionesValido_Devuelve200ConNuevoResponsable() {
        // Arrange
        var dto = new CambiarResponsableDto { TrabajadorId = Guid.NewGuid() };
        var respuesta = CrearRespuesta();
        _serviceMock
            .Setup(s => s.CambiarResponsableAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<OficinaResponseDto, DomainError>(respuesta));

        // Act
        var result = await _controller.CambiarResponsable(dto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    private static OficinaResponseDto CrearRespuesta() => new(
        "Oficina Central Leganés",
        "Avenida de la Universidad 1",
        "Leganés",
        "28911",
        "+34 916 000 111",
        "oficina@alquiler.local",
        new LocalizacionDto(40.3319, -3.7635),
        DayOfWeek.Monday,
        DayOfWeek.Friday,
        new TimeOnly(9, 0),
        new TimeOnly(20, 0),
        "Lunes a Viernes de 09:00 a 20:00",
        new ResponsableResumenDto(Guid.NewGuid(), "Elena", "Sánchez", "responsable@alquiler.local", "633445566"),
        DateTime.UtcNow);
}
