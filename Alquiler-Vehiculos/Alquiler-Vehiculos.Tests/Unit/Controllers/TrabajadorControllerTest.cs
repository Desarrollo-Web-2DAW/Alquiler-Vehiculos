using Alquiler_Vehiculos.Controllers.Usuario.Trabajador;
using Alquiler_Vehiculos.Dto.Trabajadores;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Usuarios;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Services.Usuarios.Trabajadores;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Controllers;
[TestFixture]
[Category("Unit")]
public class TrabajadorControllerTests {
    private Mock<ITrabajadorService> _serviceMock = null!;
    private TrabajadorController _sut = null!;
    [SetUp]
    public void SetUp() {
        _serviceMock = new Mock<ITrabajadorService>();
        _sut = new TrabajadorController(_serviceMock.Object);
    }
    private static TrabajadorResponseDto CrearResponseDto(Guid? id = null) => new() {
        Id = id ?? Guid.NewGuid(),
        Nombre = "Laura",
        Apellidos = "García Pérez",
        Email = "laura.garcia@empresa.es",
        Telefono = "611222333",
        FechaNacimiento = new DateOnly(1998, 5, 10),
        Dni = "12345678Z",
        FechaIngreso = new DateOnly(2024, 2, 1),
        Tipo = TipoTrabajador.Operaciones,
        Activo = true
    };
    private static CreateTrabajadorDto CrearCreateDto() => new() {
        Nombre = "Laura",
        Apellidos = "García Pérez",
        Email = "laura.garcia@empresa.es",
        Password = "Password1!",
        Telefono = "611222333",
        FechaNacimiento = new DateOnly(1998, 5, 10),
        Dni = "12345678Z",
        FechaIngreso = new DateOnly(2024, 2, 1),
        Tipo = TipoTrabajador.Operaciones
    };
    private static UpdateTrabajadorDto CrearUpdateDto() => new() {
        Nombre = "Laura Editada",
        Apellidos = "García López",
        Email = "laura.nueva@empresa.es",
        Telefono = "699888777",
        FechaNacimiento = new DateOnly(1998, 5, 10),
        Dni = "12345678Z",
        FechaIngreso = new DateOnly(2024, 2, 1),
        Tipo = TipoTrabajador.Mecanico
    };
    // ── Autorización (RF-46) ─────────────────────────────────────────────
    [Test]
    public void Controller_TieneAtributoAuthorizeConPoliticaAdmin() {
        // Arrange & Act
        var authorize = typeof(TrabajadorController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();
        // Assert
        authorize.Should().NotBeNull();
        authorize!.Policy.Should().Be(PolicyNames.Admin);
    }
    // ── GetAll ───────────────────────────────────────────────────────────
    [Test]
    public async Task GetAll_ConTrabajadores_Devuelve200ConLista() {
        // Arrange
        IReadOnlyList<TrabajadorResponseDto> lista = [CrearResponseDto(), CrearResponseDto()];
        _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(lista);
        // Act
        var resultado = await _sut.GetAll();
        // Assert
        var ok = resultado.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(lista);
        _serviceMock.Verify(s => s.GetAllAsync(), Times.Once);
    }
    // ── GetById ──────────────────────────────────────────────────────────
    [Test]
    public async Task GetById_TrabajadorExiste_Devuelve200ConDto() {
        // Arrange
        var id = Guid.NewGuid();
        var dto = CrearResponseDto(id);
        _serviceMock.Setup(s => s.GetByIdAsync(id))
            .ReturnsAsync(Result.Success<TrabajadorResponseDto, DomainError>(dto));
        // Act
        var resultado = await _sut.GetById(id);
        // Assert
        var ok = resultado.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);
        _serviceMock.Verify(s => s.GetByIdAsync(id), Times.Once);
    }
    [Test]
    public async Task GetById_TrabajadorNoExiste_Devuelve404() {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetByIdAsync(id))
            .ReturnsAsync(Result.Failure<TrabajadorResponseDto, DomainError>(TrabajadorError.NotFound(id)));
        // Act
        var resultado = await _sut.GetById(id);
        // Assert
        resultado.Result.Should().BeOfType<NotFoundObjectResult>();
        _serviceMock.Verify(s => s.GetByIdAsync(id), Times.Once);
    }
    // ── Create ───────────────────────────────────────────────────────────
    [Test]
    public async Task Create_DatosValidos_Devuelve201CreatedAtAction() {
        // Arrange
        var createDto = CrearCreateDto();
        var creado = CrearResponseDto();
        _serviceMock.Setup(s => s.CreateAsync(createDto))
            .ReturnsAsync(Result.Success<TrabajadorResponseDto, DomainError>(creado));
        // Act
        var resultado = await _sut.Create(createDto);
        // Assert
        var created = resultado.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(TrabajadorController.GetById));
        created.RouteValues!["id"].Should().Be(creado.Id);
        created.Value.Should().Be(creado);
        _serviceMock.Verify(s => s.CreateAsync(createDto), Times.Once);
    }
    [Test]
    public async Task Create_ErrorDeValidacion_Devuelve400() {
        // Arrange
        var createDto = CrearCreateDto();
        var error = ValidationError.Create("Datos inválidos");
        _serviceMock.Setup(s => s.CreateAsync(createDto))
            .ReturnsAsync(Result.Failure<TrabajadorResponseDto, DomainError>(error));
        // Act
        var resultado = await _sut.Create(createDto);
        // Assert
        resultado.Result.Should().BeOfType<BadRequestObjectResult>();
        _serviceMock.Verify(s => s.CreateAsync(createDto), Times.Once);
    }
    [Test]
    public async Task Create_DniOEmailDuplicado_Devuelve409() {
        // Arrange
        var createDto = CrearCreateDto();
        _serviceMock.Setup(s => s.CreateAsync(createDto))
            .ReturnsAsync(Result.Failure<TrabajadorResponseDto, DomainError>(
                TrabajadorError.DniExistente(createDto.Dni)));
        // Act
        var resultado = await _sut.Create(createDto);
        // Assert
        resultado.Result.Should().BeOfType<ConflictObjectResult>();
        _serviceMock.Verify(s => s.CreateAsync(createDto), Times.Once);
    }
    // ── Update ───────────────────────────────────────────────────────────
    [Test]
    public async Task Update_DatosValidos_Devuelve200ConDtoActualizado() {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = CrearUpdateDto();
        var actualizado = CrearResponseDto(id);
        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto))
            .ReturnsAsync(Result.Success<TrabajadorResponseDto, DomainError>(actualizado));
        // Act
        var resultado = await _sut.Update(id, updateDto);
        // Assert
        var ok = resultado.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(actualizado);
        _serviceMock.Verify(s => s.UpdateAsync(id, updateDto), Times.Once);
    }
    [Test]
    public async Task Update_TrabajadorNoExiste_Devuelve404() {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = CrearUpdateDto();
        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto))
            .ReturnsAsync(Result.Failure<TrabajadorResponseDto, DomainError>(TrabajadorError.NotFound(id)));
        // Act
        var resultado = await _sut.Update(id, updateDto);
        // Assert
        resultado.Result.Should().BeOfType<NotFoundObjectResult>();
        _serviceMock.Verify(s => s.UpdateAsync(id, updateDto), Times.Once);
    }
    [Test]
    public async Task Update_TrabajadorInactivo_Devuelve422() {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = CrearUpdateDto();
        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto))
            .ReturnsAsync(Result.Failure<TrabajadorResponseDto, DomainError>(
                TrabajadorError.TrabajadorInactivo(id)));
        // Act
        var resultado = await _sut.Update(id, updateDto);
        // Assert
        var obj = resultado.Result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        _serviceMock.Verify(s => s.UpdateAsync(id, updateDto), Times.Once);
    }
    // ── Delete ───────────────────────────────────────────────────────────
    [Test]
    public async Task Delete_BajaExitosa_Devuelve204NoContent() {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id))
            .ReturnsAsync(UnitResult.Success<DomainError>());
        // Act
        var resultado = await _sut.Delete(id);
        // Assert
        resultado.Should().BeOfType<NoContentResult>();
        _serviceMock.Verify(s => s.DeleteAsync(id), Times.Once);
    }
    [Test]
    public async Task Delete_TrabajadorNoExiste_Devuelve404() {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id))
            .ReturnsAsync(UnitResult.Failure<DomainError>(TrabajadorError.NotFound(id)));
        // Act
        var resultado = await _sut.Delete(id);
        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>();
        _serviceMock.Verify(s => s.DeleteAsync(id), Times.Once);
    }
    [Test]
    public async Task Delete_EsResponsableOConReservasVivas_Devuelve409() {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id))
            .ReturnsAsync(UnitResult.Failure<DomainError>(TrabajadorError.EsResponsableOficina(id)));
        // Act
        var resultado = await _sut.Delete(id);
        // Assert
        resultado.Should().BeOfType<ConflictObjectResult>();
        _serviceMock.Verify(s => s.DeleteAsync(id), Times.Once);
    }
    // ── Restore ──────────────────────────────────────────────────────────
    [Test]
    public async Task Restore_TrabajadorDadoDeBaja_Devuelve200ConDto() {
        // Arrange
        var id = Guid.NewGuid();
        var restaurado = CrearResponseDto(id);
        _serviceMock.Setup(s => s.RestoreAsync(id))
            .ReturnsAsync(Result.Success<TrabajadorResponseDto, DomainError>(restaurado));
        // Act
        var resultado = await _sut.Restore(id);
        // Assert
        var ok = resultado.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(restaurado);
        _serviceMock.Verify(s => s.RestoreAsync(id), Times.Once);
    }
    [Test]
    public async Task Restore_NoEncontrado_Devuelve404() {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.RestoreAsync(id))
            .ReturnsAsync(Result.Failure<TrabajadorResponseDto, DomainError>(TrabajadorError.NotFound(id)));
        // Act
        var resultado = await _sut.Restore(id);
        // Assert
        resultado.Result.Should().BeOfType<NotFoundObjectResult>();
        _serviceMock.Verify(s => s.RestoreAsync(id), Times.Once);
    }
}