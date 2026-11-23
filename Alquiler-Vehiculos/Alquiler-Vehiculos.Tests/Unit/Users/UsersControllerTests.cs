using Alquiler_Vehiculos.Controllers.Usuario.User;
using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Usuarios;
using Alquiler_Vehiculos.Services.Usuarios.Users;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Users;

/// <summary>
/// Pruebas unitarias de <see cref="UsersController"/> doblando <see cref="IUserService"/> con Moq.
/// </summary>
[TestFixture]
[Category("Unit")]
public class UsersControllerTests {
    private Mock<IUserService> _userServiceMock = null!;
    private UsersController _controller = null!;

    [SetUp]
    public void SetUp() {
        _userServiceMock = new Mock<IUserService>();
        _controller = new UsersController(_userServiceMock.Object, NullLogger<UsersController>.Instance);
    }

    [Test]
    public async Task GetAll_ConUsuarios_Devuelve200ConLista() {
        // Arrange
        IReadOnlyList<UserDetalleDto> usuarios = [
            CrearDetalle(Guid.NewGuid(), "admin@test.local", "Admin")
        ];
        _userServiceMock
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<UserDetalleDto>, DomainError>(usuarios));

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(usuarios);
    }

    [Test]
    public async Task GetById_Existente_Devuelve200ConDetalle() {
        // Arrange
        var id = Guid.NewGuid();
        var detalle = CrearDetalle(id, "trabajador@test.local", "Trabajador");
        _userServiceMock
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserDetalleDto, DomainError>(detalle));

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(detalle);
    }

    [Test]
    public async Task GetById_Inexistente_Devuelve404NotFound() {
        // Arrange
        var id = Guid.NewGuid();
        _userServiceMock
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserDetalleDto, DomainError>(UsuarioError.NotFound(id)));

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task CambiarRol_Valido_Devuelve200ConNuevoRol() {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new CambiarRolDto { Role = "Responsable" };
        var actualizado = CrearDetalle(id, "trabajador@test.local", "Responsable");
        _userServiceMock
            .Setup(s => s.CambiarRolAsync(id, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserDetalleDto, DomainError>(actualizado));

        // Act
        var result = await _controller.CambiarRol(id, dto, CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(actualizado);
    }

    [Test]
    public async Task CambiarRol_IncompatibleConPersona_Devuelve409Conflict() {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new CambiarRolDto { Role = "Admin" };
        _userServiceMock
            .Setup(s => s.CambiarRolAsync(id, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserDetalleDto, DomainError>(
                UsuarioError.RolIncompatibleConPersona("Admin", id)));

        // Act
        var result = await _controller.CambiarRol(id, dto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Test]
    public async Task Desactivar_CuentaValida_Devuelve204NoContent() {
        // Arrange
        var id = Guid.NewGuid();
        _userServiceMock
            .Setup(s => s.DesactivarAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        // Act
        var result = await _controller.Desactivar(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task Desactivar_UltimoAdmin_Devuelve409Conflict() {
        // Arrange
        var id = Guid.NewGuid();
        _userServiceMock
            .Setup(s => s.DesactivarAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure<DomainError>(UsuarioError.UltimoAdmin()));

        // Act
        var result = await _controller.Desactivar(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Test]
    public async Task Reactivar_CuentaExistente_Devuelve200ConDetalle() {
        // Arrange
        var id = Guid.NewGuid();
        var detalle = CrearDetalle(id, "cliente@test.local", "Cliente");
        _userServiceMock
            .Setup(s => s.ReactivarAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserDetalleDto, DomainError>(detalle));

        // Act
        var result = await _controller.Reactivar(id, CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(detalle);
    }

    private static UserDetalleDto CrearDetalle(Guid id, string email, string role) {
        return new UserDetalleDto(
            id,
            email,
            "Nombre",
            "Apellidos",
            role,
            false,
            null,
            null,
            null,
            DateTime.UtcNow);
    }
}
