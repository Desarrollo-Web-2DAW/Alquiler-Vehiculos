using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Usuarios;
using Alquiler_Vehiculos.Repositories.Clientes;
using Alquiler_Vehiculos.Services.Usuarios.Auth;
using Alquiler_Vehiculos.Tests.Unit.Infrastructure;
using Alquiler_Vehiculos.Validators.Usuarios;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Services;

[TestFixture]
[Category("Unit")]
public class AuthServiceTests {
    private Mock<UserManager<User>> _users = null!;
    private Mock<IClienteRepository> _clientes = null!;
    private AuthService _service = null!;
    private readonly string _password = Guid.NewGuid() + "aA1!";

    [SetUp]
    public void SetUp() {
        _users = IdentityManagerMocks.Usuarios();
        _clientes = new Mock<IClienteRepository>();
        var identity = Options.Create(new IdentityOptions());
        var signIn = new SignInManager<User>(_users.Object, new HttpContextAccessor(),
            new UserClaimsPrincipalFactory<User>(_users.Object, identity), identity,
            NullLogger<SignInManager<User>>.Instance,
            new AuthenticationSchemeProvider(Options.Create(new AuthenticationOptions())), new DefaultUserConfirmation<User>());
        _service = new AuthService(_users.Object, signIn, _clientes.Object,
            Options.Create(new JwtSettings { Key = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"), Issuer = "tests", Audience = "tests" }),
            new SignupValidator(), new LoginValidator(), NullLogger<AuthService>.Instance);
    }

    private SignupDto Alta() => new() { Email = "nueva@example.com", Password = _password,
        Nombre = " Lucia ", Apellidos = " Fuertes ", Telefono = "600123456", FechaNacimiento = new(2000, 1, 1), Dni = " 12345678z " };

    [TestCase(false)]
    [TestCase(true)]
    public async Task SignUpAsync_ErrorIdentity_AgrupaErroresYDeshaceSoloSiFallaRol(bool fallaRol) {
        // Arrange
        var dto = Alta();
        var fallo = IdentityResult.Failed(
            new IdentityError { Code = "Password", Description = "Primer error" },
            new IdentityError { Code = "Password", Description = "Segundo error" },
            new IdentityError { Code = "Cuenta", Description = "Cuenta rechazada" });
        _users.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _clientes.Setup(c => c.DocumentoEnUsoAsync("12345678Z", null, null, default)).ReturnsAsync(false);
        _users.Setup(u => u.CreateAsync(It.IsAny<User>(), dto.Password)).ReturnsAsync(fallaRol ? IdentityResult.Success : fallo);
        _users.Setup(u => u.AddToRoleAsync(It.IsAny<User>(), "Cliente")).ReturnsAsync(fallo);
        _users.Setup(u => u.DeleteAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);

        // Act
        var resultado = await _service.SignUpAsync(dto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        var error = resultado.Error.Should().BeOfType<ValidationError>().Subject;
        error.ValidationErrors.Should().BeEquivalentTo(new Dictionary<string, string[]> {
            ["Password"] = ["Primer error", "Segundo error"], ["Cuenta"] = ["Cuenta rechazada"]
        });
        _users.Verify(u => u.FindByEmailAsync(dto.Email), Times.Once);
        _clientes.Verify(c => c.DocumentoEnUsoAsync("12345678Z", null, null, default), Times.Once);
        _users.Verify(u => u.CreateAsync(It.Is<User>(u => u.Email == dto.Email && u.Cliente!.Dni == "12345678Z"), dto.Password), Times.Once);
        _users.Verify(u => u.AddToRoleAsync(It.IsAny<User>(), "Cliente"), fallaRol ? Times.Once() : Times.Never());
        _users.Verify(u => u.DeleteAsync(It.IsAny<User>()), fallaRol ? Times.Once() : Times.Never());
    }

    [TestCase("nie", true)]
    [TestCase("pasaporte", true)]
    [TestCase("nie", false)]
    [TestCase("pasaporte", false)]
    public async Task SignUpAsync_DocumentoAlternativo_NormalizaYDetectaDuplicados(string documento, bool duplicado) {
        // Arrange
        var dto = Alta() with { Dni = " ", Nie = documento == "nie" ? " x1234567l " : null,
            Pasaporte = documento == "pasaporte" ? " abc123 " : null, NumeroPermisoConducir = " permiso123 " };
        var nie = documento == "nie" ? "X1234567L" : null;
        var pasaporte = documento == "pasaporte" ? "ABC123" : null;
        _users.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _clientes.Setup(c => c.DocumentoEnUsoAsync(null, nie, pasaporte, default)).ReturnsAsync(duplicado);
        _users.Setup(u => u.CreateAsync(It.IsAny<User>(), dto.Password)).ReturnsAsync(IdentityResult.Success);
        _users.Setup(u => u.AddToRoleAsync(It.IsAny<User>(), "Cliente")).ReturnsAsync(IdentityResult.Success);

        // Act
        var resultado = await _service.SignUpAsync(dto);

        // Assert
        resultado.IsSuccess.Should().Be(!duplicado);
        if (duplicado) {
            resultado.Error.Code.Should().Be("DOCUMENTO_DUPLICADO");
            resultado.Error.Message.Should().Be($"Ya existe una persona con el documento '{nie ?? pasaporte}'");
        } else {
            resultado.Value.Role.Should().Be("Cliente");
            resultado.Value.Email.Should().Be(dto.Email);
            resultado.Value.Token.Should().NotBeEmpty();
        }
        _users.Verify(u => u.FindByEmailAsync(dto.Email), Times.Once);
        _clientes.Verify(c => c.DocumentoEnUsoAsync(null, nie, pasaporte, default), Times.Once);
        _users.Verify(u => u.CreateAsync(It.Is<User>(u => u.Cliente!.Dni == null && u.Cliente.Nie == nie &&
            u.Cliente.Pasaporte == pasaporte && u.Cliente.NumeroPermisoConducir == "PERMISO123"), dto.Password), duplicado ? Times.Never() : Times.Once());
        _users.Verify(u => u.AddToRoleAsync(It.IsAny<User>(), "Cliente"), duplicado ? Times.Never() : Times.Once());
        _users.Verify(u => u.DeleteAsync(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public async Task Login_DatosInvalidos_DevuelveErroresSinBuscarCuenta() {
        // Arrange
        var dto = new LoginDto { Email = "no-es-email", Password = "" };

        // Act
        var resultado = await _service.Login(dto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>().Which.ValidationErrors.Keys.Should().Contain(["Email", "Password"]);
        _users.Verify(u => u.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        _clientes.Verify(c => c.DocumentoEnUsoAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
