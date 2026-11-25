using Alquiler_Vehiculos.Controllers.Vehiculo;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Query;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Services.Vehiculo;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace Alquiler_Vehiculos.Tests.Unit.Controllers;

/// <summary>Prueba las acciones directamente; no sustituye pruebas HTTP de autenticación y permisos.</summary>
[TestFixture]
[Category("Unit")]
public class VehiculoControllerTests {
    private const string Id = "Abcdef123_-";
    private Mock<IVehiculoService> _service = null!;
    private VehiculoController _controller = null!;

    [SetUp]
    public void SetUp() {
        _service = new Mock<IVehiculoService>();
        _controller = new VehiculoController(_service.Object);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetAll_ConOSinDatos_DevuelveColeccion(bool vacia) {
        // Arrange
        VehiculoDto[] items = vacia ? [] : [new() { Id = Id }];
        _service.Setup(s => s.GetAll()).ReturnsAsync(items);

        // Act
        var resultado = await _controller.GetAll();

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(items);
        _service.Verify(s => s.GetAll(), Times.Once);
    }

    [TestCase(0, 10)]
    [TestCase(-1, 10)]
    [TestCase(1, 0)]
    [TestCase(1, -1)]
    public async Task GetPaged_ParametrosInvalidos_Devuelve400SinConsultar(int page, int size) {
        // Arrange
        var controller = _controller;

        // Act
        var resultado = await controller.GetPaged(page, size);

        // Assert
        resultado.Result.Should().BeOfType<BadRequestObjectResult>();
        _service.Verify(s => s.GetPaged(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [TestCase(0, 0)]
    [TestCase(21, 3)]
    public async Task GetPaged_ParametrosValidos_DevuelveMetadatos(int total, int paginas) {
        // Arrange
        VehiculoDto[] items = total == 0 ? [] : [new() { Id = Id }];
        _service.Setup(s => s.GetPaged(1, 10)).ReturnsAsync((items, total));

        // Act
        var resultado = await _controller.GetPaged();

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(new {
            Items = items, TotalItems = total, Page = 1, PageSize = 10, TotalPages = paginas
        });
        _service.Verify(s => s.GetPaged(1, 10), Times.Once);
        _service.Verify(s => s.GetAll(), Times.Never);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ConsultarORestaurar_Exito_Devuelve200ConDto(bool restaurar) {
        // Arrange
        var dto = new VehiculoDto { Id = Id, IsActivo = true, Estado = nameof(EstadoVehiculo.Disponible) };
        if (restaurar) _service.Setup(s => s.Restore(Id)).ReturnsAsync(Result.Success<VehiculoDto, DomainError>(dto));
        else _service.Setup(s => s.GetById(Id)).ReturnsAsync(Result.Success<VehiculoDto, DomainError>(dto));

        // Act
        var resultado = restaurar ? await _controller.Restore(Id) : await _controller.GetById(Id);

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(dto);
        _service.Verify(s => s.Restore(Id), restaurar ? Times.Once() : Times.Never());
        _service.Verify(s => s.GetById(Id), restaurar ? Times.Never() : Times.Once());
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Guardar_ArchivoCorrecto_ConstruyeDtoCompletoYDevuelveExito(bool actualizar) {
        // Arrange
        using var contenido = new MemoryStream([1, 2, 3]);
        IFormFile foto = new FormFile(contenido, 0, contenido.Length, "foto", "coche.jpg");
        var response = new VehiculoDto { Id = Id };
        CreateVehiculoDto? alta = null;
        UpdateVehiculoDto? cambio = null;
        if (actualizar)
            _service.Setup(s => s.Update(It.IsAny<UpdateVehiculoDto>(), Id)).Callback<UpdateVehiculoDto, string>((d, _) => cambio = d)
                .ReturnsAsync(Result.Success<VehiculoDto, DomainError>(response));
        else
            _service.Setup(s => s.Create(It.IsAny<CreateVehiculoDto>())).Callback<CreateVehiculoDto>(d => alta = d)
                .ReturnsAsync(Result.Success<VehiculoDto, DomainError>(response));

        // Act
        var resultado = await Guardar(actualizar, foto);

        // Assert
        object? dto = actualizar ? cambio : alta;
        dto.Should().BeEquivalentTo(new {
            Matricula = "1234BCD", Marca = "Renault", Modelo = "Megane", Acriss = "CDMD",
            Color = "Rojo", PrecioBaseDia = 50m, Plazas = 5, MaleteroLitros = 400, AutonomiaKm = 600,
            Kilometraje = 12000, FechaUltimaRevision = "2026-01-01", Concesionario = 1L
        });
        (actualizar ? cambio!.Foto : alta!.Foto).Should().BeSameAs(foto);
        if (actualizar) resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(response);
        else {
            var creado = resultado.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            creado.ActionName.Should().Be(nameof(VehiculoController.GetById));
            creado.RouteValues!["id"].Should().Be(Id);
            creado.Value.Should().BeSameAs(response);
        }
        _service.Verify(s => s.Create(It.IsAny<CreateVehiculoDto>()), actualizar ? Times.Never() : Times.Once());
        _service.Verify(s => s.Update(It.IsAny<UpdateVehiculoDto>(), Id), actualizar ? Times.Once() : Times.Never());
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Delete_Exito_ConservaBooleano(bool eliminado) {
        // Arrange
        _service.Setup(s => s.SoftDelete(Id)).ReturnsAsync(Result.Success<bool, DomainError>(eliminado));

        // Act
        var resultado = await _controller.Delete(Id);

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(eliminado);
        _service.Verify(s => s.SoftDelete(Id), Times.Once);
    }

    private static IEnumerable<TestCaseData> Errores() {
        foreach (var operacion in new[] { "id", "restore", "create", "update", "delete" }) {
            yield return new TestCaseData(operacion, ValidationError.Create("Dato inválido"), 400);
            yield return new TestCaseData(operacion, new NotFoundError("NO_EXISTE", "No existe"), 404);
            yield return new TestCaseData(operacion, new ConflictError("CONFLICTO", "En uso"), 409);
            yield return new TestCaseData(operacion, new BusinessRuleError("REGLA", "No permitido"), 422);
            yield return new TestCaseData(operacion, new InternalError("Fallo interno"), 500);
            yield return new TestCaseData(operacion, new UnauthorizedError("SIN_SESION", "Sin identificar"), 401);
            yield return new TestCaseData(operacion, new ForbiddenError("SIN_PERMISO", "Sin permiso"), 403);
        }
        yield return new TestCaseData("create", VehiculoError.VehiculoYaExiste("1234BCD"), 409);
        yield return new TestCaseData("update", VehiculoError.VehiculoYaExiste("1234BCD"), 409);
        yield return new TestCaseData("delete", VehiculoError.NoDisponibleParaBaja(EstadoVehiculo.Reservado), 409);
        yield return new TestCaseData("delete", VehiculoError.VehiculoConPartesAbiertos(Id), 409);
    }

    [TestCaseSource(nameof(Errores))]
    public async Task Operacion_ErrorDominio_ConservaCodigoHttp(string operacion, DomainError error, int status) {
        // Arrange
        using var contenido = new MemoryStream([1]);
        IFormFile foto = new FormFile(contenido, 0, 1, "foto", "coche.jpg");
        var fallo = Result.Failure<VehiculoDto, DomainError>(error);
        switch (operacion) {
            case "id": _service.Setup(s => s.GetById(Id)).ReturnsAsync(fallo); break;
            case "restore": _service.Setup(s => s.Restore(Id)).ReturnsAsync(fallo); break;
            case "create": _service.Setup(s => s.Create(It.IsAny<CreateVehiculoDto>())).ReturnsAsync(fallo); break;
            case "update": _service.Setup(s => s.Update(It.IsAny<UpdateVehiculoDto>(), Id)).ReturnsAsync(fallo); break;
            case "delete": _service.Setup(s => s.SoftDelete(Id)).ReturnsAsync(Result.Failure<bool, DomainError>(error)); break;
        }
        // Act
        var resultado = operacion switch {
            "id" => (await _controller.GetById(Id)).Result,
            "restore" => (await _controller.Restore(Id)).Result,
            "delete" => (await _controller.Delete(Id)).Result,
            _ => (await Guardar(operacion == "update", foto)).Result
        };

        // Assert
        resultado.Should().BeAssignableTo<ObjectResult>().Which.StatusCode.Should().Be(status);
        _service.Verify(s => s.GetById(Id), operacion == "id" ? Times.Once() : Times.Never());
        _service.Verify(s => s.Restore(Id), operacion == "restore" ? Times.Once() : Times.Never());
        _service.Verify(s => s.SoftDelete(Id), operacion == "delete" ? Times.Once() : Times.Never());
        _service.Verify(s => s.Create(It.IsAny<CreateVehiculoDto>()), operacion == "create" ? Times.Once() : Times.Never());
        _service.Verify(s => s.Update(It.IsAny<UpdateVehiculoDto>(), Id), operacion == "update" ? Times.Once() : Times.Never());
    }

    [Test]
    public async Task GetByMatricula_NoExiste_Devuelve404() {
        // Arrange
        _service.Setup(s => s.GetByMatricula("1234BCD"))
            .ReturnsAsync(Result.Failure<VehiculoDto, DomainError>(VehiculoError.NotFoundVehiculoMatricula("1234BCD")));

        // Act
        var resultado = await _controller.GetByMatricula("1234BCD");

        // Assert
        resultado.Result.Should().BeOfType<NotFoundObjectResult>();
        _service.Verify(s => s.GetByMatricula("1234BCD"), Times.Once);
    }

    [TestCase(false, false, 400)]
    [TestCase(true, false, 400)]
    [TestCase(false, true, 500)]
    [TestCase(true, true, 500)]
    public async Task GetByFilters_ErrorServicio_ConservaCodigoYPerfil(bool admin, bool pendiente, int status) {
        // Arrange
        var request = new QueryRequestVehiculoDto { Categoria = "NoValida" };
        var claims = admin
            ? new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin") }
            : Array.Empty<System.Security.Claims.Claim>();
        _controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims, admin ? "Test" : null))
            }
        };
        DomainError error = pendiente ? new InternalError("Integración pendiente") : ValidationError.Create("Categoría inválida");
        _service.Setup(s => s.GetByFilters(request, admin))
            .ReturnsAsync(Result.Failure<IEnumerable<QueryResponseVehiculoDto>, DomainError>(error));

        // Act
        var resultado = await _controller.GetByFilters(request);

        // Assert
        resultado.Result.Should().BeAssignableTo<ObjectResult>().Which.StatusCode.Should().Be(status);
        _service.Verify(s => s.GetByFilters(request, admin), Times.Once);
    }

    private static IEnumerable<TestCaseData> CasosVisibilidad() {
        foreach (var porMatricula in new[] { false, true })
        foreach (var perfil in new[] {
            (Rol: "", Autenticado: false), (Rol: "Cliente", Autenticado: true),
            (Rol: "Trabajador", Autenticado: true), (Rol: "Responsable", Autenticado: true),
            (Rol: "Admin", Autenticado: true), (Rol: "Admin", Autenticado: false)
        })
        foreach (var estado in Enum.GetValues<EstadoVehiculo>())
        foreach (var activo in new[] { false, true }) {
            var personal = perfil.Autenticado && perfil.Rol is "Trabajador" or "Responsable" or "Admin";
            var visible = personal || (activo && estado == EstadoVehiculo.Disponible);
            yield return new TestCaseData(porMatricula, perfil.Rol, perfil.Autenticado, estado, activo, visible);
        }
    }

    [TestCaseSource(nameof(CasosVisibilidad))]
    public async Task ConsultarUnidad_PerfilYEstado_RespetaVisibilidad(
        bool porMatricula, string rol, bool autenticado, EstadoVehiculo estado, bool activo, bool visible) {
        // Arrange
        var dto = new VehiculoDto { Id = Id, Matricula = "1234BCD", IsActivo = activo, Estado = estado.ToString() };
        _controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Role, rol)], autenticado ? "Test" : null))
            }
        };
        if (porMatricula) _service.Setup(s => s.GetByMatricula(dto.Matricula)).ReturnsAsync(Result.Success<VehiculoDto, DomainError>(dto));
        else _service.Setup(s => s.GetById(Id)).ReturnsAsync(Result.Success<VehiculoDto, DomainError>(dto));

        // Act
        var resultado = porMatricula ? await _controller.GetByMatricula(dto.Matricula) : await _controller.GetById(Id);

        // Assert
        if (visible) resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(dto);
        else {
            var error = porMatricula ? VehiculoError.NotFoundVehiculoMatricula(dto.Matricula) : VehiculoError.NotFoundVehiculo(Id);
            resultado.Result.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().BeEquivalentTo(new {
                code = error.Code, message = error.Message
            });
        }
        _service.Verify(s => s.GetById(Id), porMatricula ? Times.Never() : Times.Once());
        _service.Verify(s => s.GetByMatricula(dto.Matricula), porMatricula ? Times.Once() : Times.Never());
    }

    private Task<ActionResult<VehiculoDto>> Guardar(bool actualizar, IFormFile foto) => actualizar
        ? _controller.Update(Id, foto, "1234BCD", "Renault", "Megane", "CDMD", "Rojo", 50m, 5, 400, 600, 12000, "2026-01-01", 1)
        : _controller.Create(foto, "1234BCD", "Renault", "Megane", "CDMD", "Rojo", 50m, 5, 400, 600, 12000, "2026-01-01", 1);
}
