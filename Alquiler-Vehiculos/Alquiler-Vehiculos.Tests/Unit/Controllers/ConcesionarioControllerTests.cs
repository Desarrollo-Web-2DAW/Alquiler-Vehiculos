using Alquiler_Vehiculos.Controllers.Vehiculo;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Concesionario;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Services.Vehiculo.Concesonario;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Controllers;

[TestFixture]
[Category("Unit")]
public class ConcesionarioControllerTests {
    private const long Id = 5;
    private Mock<IConcesionarioService> _service = null!;
    private ConcesionarioController _controller = null!;

    [SetUp]
    public void SetUp() {
        _service = new Mock<IConcesionarioService>();
        _controller = new ConcesionarioController(_service.Object);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetAll_ConYSinConcesionarios_Devuelve200ConColeccion(bool vacia) {
        // Arrange
        ConcesionarioDto[] concesionarios = vacia ? [] : [new() { Id = Id, Nombre = "Motor Norte" }];
        _service.Setup(s => s.GetAll()).ReturnsAsync(concesionarios);

        // Act
        var resultado = await _controller.GetAll();

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(concesionarios);
        _service.Verify(s => s.GetAll(), Times.Once);
    }

    [Test]
    public async Task GetById_Existente_Devuelve200ConDto() {
        // Arrange
        var concesionario = new ConcesionarioDto { Id = Id };
        _service.Setup(s => s.GetById(Id)).ReturnsAsync(Result.Success<ConcesionarioDto, DomainError>(concesionario));

        // Act
        var resultado = await _controller.GetById(Id);

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(concesionario);
        _service.Verify(s => s.GetById(Id), Times.Once);
    }

    [Test]
    public async Task Create_AltaCorrecta_Devuelve201ConDtoYUbicacion() {
        // Arrange
        var dto = new CreateConcesionarioDto { Nombre = "Motor Norte" };
        var concesionario = new ConcesionarioDto { Id = Id, Nombre = dto.Nombre };
        _service.Setup(s => s.Create(dto)).ReturnsAsync(Result.Success<ConcesionarioDto, DomainError>(concesionario));

        // Act
        var resultado = await _controller.Create(dto);

        // Assert
        var respuesta = resultado.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        respuesta.StatusCode.Should().Be(201);
        respuesta.ActionName.Should().Be(nameof(ConcesionarioController.GetById));
        respuesta.RouteValues!["id"].Should().Be(Id);
        respuesta.Value.Should().BeSameAs(concesionario);
        _service.Verify(s => s.Create(dto), Times.Once);
    }

    [Test]
    public async Task Update_CambioCorrecto_Devuelve200ConDto() {
        // Arrange
        var dto = new UpdateConcesionarioDto { Nombre = "Motor Sur" };
        var concesionario = new ConcesionarioDto { Id = Id, Nombre = dto.Nombre };
        _service.Setup(s => s.Update(dto, Id)).ReturnsAsync(Result.Success<ConcesionarioDto, DomainError>(concesionario));

        // Act
        var resultado = await _controller.Update(Id, dto);

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(concesionario);
        _service.Verify(s => s.Update(dto, Id), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Delete_ResultadoCorrecto_Devuelve200ConBooleano(bool borrado) {
        // Arrange
        _service.Setup(s => s.SoftDelete(Id)).ReturnsAsync(Result.Success<bool, DomainError>(borrado));

        // Act
        var resultado = await _controller.Delete(Id);

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(borrado);
        _service.Verify(s => s.SoftDelete(Id), Times.Once);
    }

    [TestCase("consulta", 404)]
    [TestCase("alta", 400)]
    [TestCase("alta", 409)]
    [TestCase("cambio", 400)]
    [TestCase("cambio", 404)]
    [TestCase("cambio", 409)]
    [TestCase("baja", 404)]
    [TestCase("baja", 409)]
    public async Task Accion_ErrorDeDominio_DevuelveCodigoYCuerpo(string accion, int estado) {
        // Arrange
        DomainError error = estado switch {
            404 => new NotFoundError("CONCESIONARIO_NO_ENCONTRADO", "Concesionario ausente"),
            409 => new ConflictError("CONFLICTO_CONCESIONARIO", "Conflicto al guardar o borrar"),
            _ => ValidationError.WithFieldErrors(new() { ["Telefono"] = ["Telefono incorrecto"] })
        };
        var alta = new CreateConcesionarioDto();
        var cambio = new UpdateConcesionarioDto();
        _service.Setup(s => s.GetById(Id)).ReturnsAsync(Result.Failure<ConcesionarioDto, DomainError>(error));
        _service.Setup(s => s.Create(alta)).ReturnsAsync(Result.Failure<ConcesionarioDto, DomainError>(error));
        _service.Setup(s => s.Update(cambio, Id)).ReturnsAsync(Result.Failure<ConcesionarioDto, DomainError>(error));
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
