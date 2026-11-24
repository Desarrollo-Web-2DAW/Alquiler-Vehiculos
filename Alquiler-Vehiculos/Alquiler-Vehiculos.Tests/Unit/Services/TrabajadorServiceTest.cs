using Alquiler_Vehiculos.Dto.Trabajadores;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using Alquiler_Vehiculos.Repositories.Trabajadores;
using Alquiler_Vehiculos.Services.Usuarios.Trabajadores;
using Alquiler_Vehiculos.Validators.Trabajadores;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Services;

[TestFixture]
[Category("Unit")]
public class TrabajadorServiceTests {
    private Mock<ITrabajadorRepository> _repoMock = null!;
    private Mock<UserManager<User>> _userManagerMock = null!;
    private TrabajadorService _sut = null!;
    [SetUp]
    public void SetUp() {
        _repoMock = new Mock<ITrabajadorRepository>();
        var userStoreMock = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);
        _sut = new TrabajadorService(
            _repoMock.Object,
            _userManagerMock.Object,
            new CreateTrabajadorValidator(),
            new UpdateTrabajadorValidator(),
            NullLogger<TrabajadorService>.Instance);
    }
    private static Trabajador CrearTrabajador(
        Guid? id = null,
        string email = "laura.garcia@empresa.es",
        string dni = "12345678Z",
        bool activo = true) => new() {
        Id = id ?? Guid.NewGuid(),
        Nombre = "Laura",
        Apellidos = "García Pérez",
        Email = email,
        Telefono = "611222333",
        FechaNacimiento = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-25),
        Dni = dni,
        FechaIngreso = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-1),
        Tipo = TipoTrabajador.Operaciones,
        Activo = activo
    };
    private static CreateTrabajadorDto CrearCreateDtoValido() => new() {
        Nombre = "Laura",
        Apellidos = "García Pérez",
        Email = "LAURA.GARCIA@empresa.es",
        Password = "Password1!",
        Telefono = "611222333",
        FechaNacimiento = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-25),
        Dni = "12345678z",
        FechaIngreso = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-1),
        Tipo = TipoTrabajador.Operaciones
    };
    private static UpdateTrabajadorDto CrearUpdateDtoValido(
        string email = "laura.garcia@empresa.es",
        string dni = "12345678Z") => new() {
        Nombre = "Laura Editada",
        Apellidos = "García López",
        Email = email,
        Telefono = "699888777",
        FechaNacimiento = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-25),
        Dni = dni,
        FechaIngreso = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-1),
        Tipo = TipoTrabajador.Mecanico
    };
    // ── GetAllAsync / GetByIdAsync ───────────────────────────────────────
    [Test]
    public async Task GetAllAsync_ConTrabajadores_DevuelveListaMapeada() {
        // Arrange
        var lista = new List<Trabajador> { CrearTrabajador(), CrearTrabajador(email: "carlos@empresa.es", dni: "11111111H") };
        _repoMock.Setup(r => r.GetAll()).ReturnsAsync(lista);
        // Act
        var resultado = await _sut.GetAllAsync();
        // Assert
        resultado.Should().HaveCount(2);
        resultado[0].Email.Should().Be("laura.garcia@empresa.es");
        _repoMock.Verify(r => r.GetAll(), Times.Once);
    }
    [Test]
    public async Task GetByIdAsync_TrabajadorExiste_DevuelveDto() {
        // Arrange
        var id = Guid.NewGuid();
        var trabajador = CrearTrabajador(id: id);
        _repoMock.Setup(r => r.GetById(id)).ReturnsAsync(trabajador);
        // Act
        var resultado = await _sut.GetByIdAsync(id);
        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().Be(id);
        resultado.Value.Dni.Should().Be("12345678Z");
        _repoMock.Verify(r => r.GetById(id), Times.Once);
    }
    [Test]
    public async Task GetByIdAsync_TrabajadorNoExiste_DevuelveNotFound() {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetById(id)).ReturnsAsync((Trabajador?)null);
        // Act
        var resultado = await _sut.GetByIdAsync(id);
        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(TrabajadorError.NotFound(id));
        _repoMock.Verify(r => r.GetById(id), Times.Once);
    }
    // ── CreateAsync ──────────────────────────────────────────────────────
    [Test]
    public async Task CreateAsync_DatosValidos_CreaUsuarioYAsignaRolTrabajador() {
        // Arrange
        var dto = CrearCreateDtoValido();
        _repoMock.Setup(r => r.DniEnUso("12345678Z", null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.EmailEnUso("laura.garcia@empresa.es", null)).ReturnsAsync(false);
        _userManagerMock.Setup(u => u.FindByEmailAsync("laura.garcia@empresa.es")).ReturnsAsync((User?)null);
        _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<User>(), dto.Password)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(u => u.AddToRoleAsync(It.IsAny<User>(), nameof(RolUsuario.Trabajador)))
            .ReturnsAsync(IdentityResult.Success);
        // Act
        var resultado = await _sut.CreateAsync(dto);
        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Email.Should().Be("laura.garcia@empresa.es");
        resultado.Value.Dni.Should().Be("12345678Z");
        _repoMock.Verify(r => r.DniEnUso("12345678Z", null), Times.Once);
        _repoMock.Verify(r => r.EmailEnUso("laura.garcia@empresa.es", null), Times.Once);
        _userManagerMock.Verify(u => u.FindByEmailAsync("laura.garcia@empresa.es"), Times.Once);
        _userManagerMock.Verify(u => u.CreateAsync(
            It.Is<User>(usr => usr.Email == "laura.garcia@empresa.es" && usr.Trabajador != null),
            dto.Password), Times.Once);
        _userManagerMock.Verify(u => u.AddToRoleAsync(It.IsAny<User>(), nameof(RolUsuario.Trabajador)), Times.Once);
    }
    [Test]
    public async Task CreateAsync_DtoInvalido_DevuelveValidationErrorYNoLlamaRepoNiIdentity() {
        // Arrange
        var dto = CrearCreateDtoValido();
        dto.Dni = "12345678A"; // Letra inválida (RF-16)
        // Act
        var resultado = await _sut.CreateAsync(dto);
        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>();
        _repoMock.Verify(r => r.DniEnUso(It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
        _userManagerMock.Verify(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }
    [Test]
    public async Task CreateAsync_DniDuplicado_DevuelveConflictYNoCreaUsuario() {
        // Arrange
        var dto = CrearCreateDtoValido();
        _repoMock.Setup(r => r.DniEnUso("12345678Z", null)).ReturnsAsync(true);
        // Act
        var resultado = await _sut.CreateAsync(dto);
        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(TrabajadorError.DniExistente("12345678Z"));
        _repoMock.Verify(r => r.DniEnUso("12345678Z", null), Times.Once);
        _repoMock.Verify(r => r.EmailEnUso(It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
        _userManagerMock.Verify(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }
    [Test]
    public async Task CreateAsync_EmailDuplicadoEnIdentity_DevuelveConflictYNoCreaUsuario() {
        // Arrange
        var dto = CrearCreateDtoValido();
        _repoMock.Setup(r => r.DniEnUso("12345678Z", null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.EmailEnUso("laura.garcia@empresa.es", null)).ReturnsAsync(false);
        _userManagerMock.Setup(u => u.FindByEmailAsync("laura.garcia@empresa.es")).ReturnsAsync(new User());
        // Act
        var resultado = await _sut.CreateAsync(dto);
        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(TrabajadorError.EmailExistente("laura.garcia@empresa.es"));
        _userManagerMock.Verify(u => u.FindByEmailAsync("laura.garcia@empresa.es"), Times.Once);
        _userManagerMock.Verify(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }
    [Test]
    public async Task CreateAsync_FallaAsignacionDeRol_EliminaUsuarioYDevuelveValidationError() {
        // Arrange
        var dto = CrearCreateDtoValido();
        _repoMock.Setup(r => r.DniEnUso("12345678Z", null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.EmailEnUso("laura.garcia@empresa.es", null)).ReturnsAsync(false);
        _userManagerMock.Setup(u => u.FindByEmailAsync("laura.garcia@empresa.es")).ReturnsAsync((User?)null);
        _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<User>(), dto.Password)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(u => u.AddToRoleAsync(It.IsAny<User>(), nameof(RolUsuario.Trabajador)))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "RoleError", Description = "Error de rol" }));
        _userManagerMock.Setup(u => u.DeleteAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
        // Act
        var resultado = await _sut.CreateAsync(dto);
        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>();
        _userManagerMock.Verify(u => u.CreateAsync(It.IsAny<User>(), dto.Password), Times.Once);
        _userManagerMock.Verify(u => u.AddToRoleAsync(It.IsAny<User>(), nameof(RolUsuario.Trabajador)), Times.Once);
        _userManagerMock.Verify(u => u.DeleteAsync(It.IsAny<User>()), Times.Once);
    }
    // ── UpdateAsync ──────────────────────────────────────────────────────
    [Test]
    public async Task UpdateAsync_DatosValidosCambiandoEmail_ActualizaTrabajadorYUser() {
        // Arrange
        var id = Guid.NewGuid();
        var existente = CrearTrabajador(id: id, email: "anterior@empresa.es");
        var dto = CrearUpdateDtoValido(email: "nuevo@empresa.es");
        var user = new User { Email = "anterior@empresa.es", UserName = "anterior@empresa.es", TrabajadorId = id };
        var actualizado = CrearTrabajador(id: id, email: "nuevo@empresa.es");
        actualizado.Tipo = TipoTrabajador.Mecanico;
        _repoMock.Setup(r => r.GetById(id)).ReturnsAsync(existente);
        _repoMock.Setup(r => r.DniEnUso("12345678Z", id)).ReturnsAsync(false);
        _repoMock.Setup(r => r.EmailEnUso("nuevo@empresa.es", id)).ReturnsAsync(false);
        _userManagerMock.Setup(u => u.FindByEmailAsync("nuevo@empresa.es")).ReturnsAsync((User?)null);
        _userManagerMock.Setup(u => u.FindByEmailAsync("anterior@empresa.es")).ReturnsAsync(user);
        _userManagerMock.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _repoMock.Setup(r => r.Update(id, It.IsAny<Trabajador>())).ReturnsAsync(actualizado);
        // Act
        var resultado = await _sut.UpdateAsync(id, dto);
        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Email.Should().Be("nuevo@empresa.es");
        _userManagerMock.Verify(u => u.UpdateAsync(
            It.Is<User>(usr => usr.Email == "nuevo@empresa.es" && usr.UserName == "nuevo@empresa.es")), Times.Once);
        _repoMock.Verify(r => r.Update(id, It.Is<Trabajador>(t => t.Email == "nuevo@empresa.es")), Times.Once);
    }
    [Test]
    public async Task UpdateAsync_TrabajadorNoExiste_DevuelveNotFoundYNoActualiza() {
        // Arrange
        var id = Guid.NewGuid();
        var dto = CrearUpdateDtoValido();
        _repoMock.Setup(r => r.GetById(id)).ReturnsAsync((Trabajador?)null);
        // Act
        var resultado = await _sut.UpdateAsync(id, dto);
        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(TrabajadorError.NotFound(id));
        _repoMock.Verify(r => r.GetById(id), Times.Once);
        _repoMock.Verify(r => r.Update(It.IsAny<Guid>(), It.IsAny<Trabajador>()), Times.Never);
    }
    [Test]
    public async Task UpdateAsync_TrabajadorInactivo_DevuelveBusinessRuleErrorYNoActualiza() {
        // Arrange
        var id = Guid.NewGuid();
        var inactivo = CrearTrabajador(id: id, activo: false);
        var dto = CrearUpdateDtoValido();
        _repoMock.Setup(r => r.GetById(id)).ReturnsAsync(inactivo);
        // Act
        var resultado = await _sut.UpdateAsync(id, dto);
        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(TrabajadorError.TrabajadorInactivo(id));
        _repoMock.Verify(r => r.GetById(id), Times.Once);
        _repoMock.Verify(r => r.Update(It.IsAny<Guid>(), It.IsAny<Trabajador>()), Times.Never);
    }
    [Test]
    public async Task UpdateAsync_EmailEnUsoPorOtroUsuario_DevuelveConflictYNoActualiza() {
        // Arrange
        var id = Guid.NewGuid();
        var existente = CrearTrabajador(id: id, email: "laura@empresa.es");
        var dto = CrearUpdateDtoValido(email: "otro@empresa.es");
        var userDeOtro = new User { Email = "otro@empresa.es", TrabajadorId = Guid.NewGuid() };
        _repoMock.Setup(r => r.GetById(id)).ReturnsAsync(existente);
        _repoMock.Setup(r => r.DniEnUso("12345678Z", id)).ReturnsAsync(false);
        _repoMock.Setup(r => r.EmailEnUso("otro@empresa.es", id)).ReturnsAsync(false);
        _userManagerMock.Setup(u => u.FindByEmailAsync("otro@empresa.es")).ReturnsAsync(userDeOtro);
        // Act
        var resultado = await _sut.UpdateAsync(id, dto);
        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(TrabajadorError.EmailExistente("otro@empresa.es"));
        _repoMock.Verify(r => r.Update(It.IsAny<Guid>(), It.IsAny<Trabajador>()), Times.Never);
    }
    // ── DeleteAsync ──────────────────────────────────────────────────────
    [Test]
    public async Task DeleteAsync_SinBloqueos_DaDeBajaLogica() {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetById(id)).ReturnsAsync(CrearTrabajador(id: id));
        _repoMock.Setup(r => r.EsResponsableOficinaAsync(id)).ReturnsAsync(false);
        _repoMock.Setup(r => r.TieneReservasVivasAsync(id)).ReturnsAsync(false);
        _repoMock.Setup(r => r.Delete(id)).ReturnsAsync(true);
        // Act
        var resultado = await _sut.DeleteAsync(id);
        // Assert
        resultado.IsSuccess.Should().BeTrue();
        _repoMock.Verify(r => r.EsResponsableOficinaAsync(id), Times.Once);
        _repoMock.Verify(r => r.TieneReservasVivasAsync(id), Times.Once);
        _repoMock.Verify(r => r.Delete(id), Times.Once);
    }
    [Test]
    public async Task DeleteAsync_TrabajadorNoExiste_DevuelveNotFoundYNoElimina() {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetById(id)).ReturnsAsync((Trabajador?)null);
        // Act
        var resultado = await _sut.DeleteAsync(id);
        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(TrabajadorError.NotFound(id));
        _repoMock.Verify(r => r.EsResponsableOficinaAsync(It.IsAny<Guid>()), Times.Never);
        _repoMock.Verify(r => r.Delete(It.IsAny<Guid>()), Times.Never);
    }
    [Test]
    public async Task DeleteAsync_EsResponsableOficina_DevuelveConflictYNoElimina() {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetById(id)).ReturnsAsync(CrearTrabajador(id: id));
        _repoMock.Setup(r => r.EsResponsableOficinaAsync(id)).ReturnsAsync(true);
        // Act
        var resultado = await _sut.DeleteAsync(id);
        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(TrabajadorError.EsResponsableOficina(id));
        _repoMock.Verify(r => r.EsResponsableOficinaAsync(id), Times.Once);
        _repoMock.Verify(r => r.TieneReservasVivasAsync(It.IsAny<Guid>()), Times.Never);
        _repoMock.Verify(r => r.Delete(It.IsAny<Guid>()), Times.Never);
    }
    [Test]
    public async Task DeleteAsync_TieneReservasVivas_DevuelveConflictYNoElimina() {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetById(id)).ReturnsAsync(CrearTrabajador(id: id));
        _repoMock.Setup(r => r.EsResponsableOficinaAsync(id)).ReturnsAsync(false);
        _repoMock.Setup(r => r.TieneReservasVivasAsync(id)).ReturnsAsync(true);
        // Act
        var resultado = await _sut.DeleteAsync(id);
        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(TrabajadorError.ConReservasVivas(id));
        _repoMock.Verify(r => r.TieneReservasVivasAsync(id), Times.Once);
        _repoMock.Verify(r => r.Delete(It.IsAny<Guid>()), Times.Never);
    }
    // ── RestoreAsync ─────────────────────────────────────────────────────
    [Test]
    public async Task RestoreAsync_TrabajadorDadoDeBaja_DevuelveDtoReactivado() {
        // Arrange
        var id = Guid.NewGuid();
        var restaurado = CrearTrabajador(id: id);
        _repoMock.Setup(r => r.Restore(id)).ReturnsAsync(restaurado);
        // Act
        var resultado = await _sut.RestoreAsync(id);
        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().Be(id);
        resultado.Value.Activo.Should().BeTrue();
        _repoMock.Verify(r => r.Restore(id), Times.Once);
    }
    [Test]
    public async Task RestoreAsync_TrabajadorNoEncontrado_DevuelveNotFound() {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.Restore(id)).ReturnsAsync((Trabajador?)null);
        // Act
        var resultado = await _sut.RestoreAsync(id);
        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(TrabajadorError.NotFound(id));
        _repoMock.Verify(r => r.Restore(id), Times.Once);
    }
}