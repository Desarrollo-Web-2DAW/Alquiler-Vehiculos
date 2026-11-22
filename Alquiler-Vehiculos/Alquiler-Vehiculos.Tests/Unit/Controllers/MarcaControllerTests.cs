using Alquiler_Vehiculos.Controllers.Vehiculo;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Marca;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Services.Vehiculo.Marca;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Controllers;

[TestFixture]
[Category("Unit")]
public class MarcaControllerTests {
    private const string Id = "507f1f77bcf86cd799439011";
    private Mock<IMarcaService> _service = null!;
    private MarcaController _controller = null!;

    [SetUp]
    public void SetUp() {
        _service = new Mock<IMarcaService>();
        _controller = new MarcaController(_service.Object);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetAll_ConYSinMarcas_Devuelve200ConLaColeccion(bool vacia) {
        // Arrange
        MarcaDto[] marcas = vacia ? [] : [new() { Id = Id, Nombre = "Seat" }];
        _service.Setup(s => s.GetAll()).ReturnsAsync(marcas);

        // Act
        var resultado = await _controller.GetAll();

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(marcas);
        _service.Verify(s => s.GetAll(), Times.Once);
    }

    [Test]
    public async Task GetById_Existente_Devuelve200ConElDto() {
        // Arrange
        var marca = new MarcaDto { Id = Id, Nombre = "Seat" };
        _service.Setup(s => s.GetById(Id)).ReturnsAsync(Result.Success<MarcaDto, DomainError>(marca));

        // Act
        var resultado = await _controller.GetById(Id);

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(marca);
        _service.Verify(s => s.GetById(Id), Times.Once);
    }

    [Test]
    public async Task Create_AltaCorrecta_Devuelve201ConDtoYUbicacion() {
        // Arrange
        var dto = new CreateMarcaDto { Nombre = "Seat", ModelosDisponibles = ["Ibiza"] };
        var marca = new MarcaDto { Id = Id, Nombre = dto.Nombre, ModelosDisponibles = dto.ModelosDisponibles };
        _service.Setup(s => s.Create(dto)).ReturnsAsync(Result.Success<MarcaDto, DomainError>(marca));

        // Act
        var resultado = await _controller.Create(dto);

        // Assert
        var respuesta = resultado.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        respuesta.StatusCode.Should().Be(201);
        respuesta.ActionName.Should().Be(nameof(MarcaController.GetById));
        respuesta.RouteValues!["id"].Should().Be(Id);
        respuesta.Value.Should().BeSameAs(marca);
        _service.Verify(s => s.Create(dto), Times.Once);
    }

    [Test]
    public async Task Update_CambioCorrecto_Devuelve200ConDto() {
        // Arrange
        var dto = new UpdateMarcaDto { Nombre = "Seat", ModelosDisponibles = ["Leon"] };
        var marca = new MarcaDto { Id = Id, Nombre = dto.Nombre, ModelosDisponibles = dto.ModelosDisponibles };
        _service.Setup(s => s.Update(dto, Id)).ReturnsAsync(Result.Success<MarcaDto, DomainError>(marca));

        // Act
        var resultado = await _controller.Update(Id, dto);

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(marca);
        _service.Verify(s => s.Update(dto, Id), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Delete_ServicioDevuelveExito_ConservaElBooleano(bool borrada) {
        // Arrange
        _service.Setup(s => s.SoftDelete(Id)).ReturnsAsync(Result.Success<bool, DomainError>(borrada));

        // Act
        var resultado = await _controller.Delete(Id);

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(borrada);
        _service.Verify(s => s.SoftDelete(Id), Times.Once);
    }

    [TestCase("consulta", 400)]
    [TestCase("consulta", 404)]
    [TestCase("alta", 400)]
    [TestCase("cambio", 400)]
    [TestCase("cambio", 404)]
    [TestCase("baja", 400)]
    [TestCase("baja", 404)]
    public async Task Accion_ErrorDeDominio_DevuelveCodigoYCuerpo(string accion, int estado) {
        // Arrange
        DomainError error = estado == 404
            ? new NotFoundError("MARCA_NO_ENCONTRADA", "Marca ausente")
            : ValidationError.WithFieldErrors(new() { ["Nombre"] = ["Nombre obligatorio"] });
        var alta = new CreateMarcaDto();
        var cambio = new UpdateMarcaDto();
        _service.Setup(s => s.GetById(Id)).ReturnsAsync(Result.Failure<MarcaDto, DomainError>(error));
        _service.Setup(s => s.Create(alta)).ReturnsAsync(Result.Failure<MarcaDto, DomainError>(error));
        _service.Setup(s => s.Update(cambio, Id)).ReturnsAsync(Result.Failure<MarcaDto, DomainError>(error));
        _service.Setup(s => s.SoftDelete(Id)).ReturnsAsync(Result.Failure<bool, DomainError>(error));

        // Act
        ActionResult respuesta = accion switch {
            "consulta" => (await _controller.GetById(Id)).Result!,
            "alta" => (await _controller.Create(alta)).Result!,
            "cambio" => (await _controller.Update(Id, cambio)).Result!,
            _ => (await _controller.Delete(Id)).Result!
        };

        // Assert
        var objeto = respuesta.Should().BeAssignableTo<ObjectResult>().Subject;
        objeto.StatusCode.Should().Be(estado);
        if (error is ValidationError validacion)
            objeto.Value.Should().BeEquivalentTo(new { code = error.Code, message = error.Message, details = validacion.ValidationErrors });
        else objeto.Value.Should().BeEquivalentTo(new { code = error.Code, message = error.Message });
        _service.Verify(s => s.GetById(Id), accion == "consulta" ? Times.Once() : Times.Never());
        _service.Verify(s => s.Create(alta), accion == "alta" ? Times.Once() : Times.Never());
        _service.Verify(s => s.Update(cambio, Id), accion == "cambio" ? Times.Once() : Times.Never());
        _service.Verify(s => s.SoftDelete(Id), accion == "baja" ? Times.Once() : Times.Never());
    }
}
