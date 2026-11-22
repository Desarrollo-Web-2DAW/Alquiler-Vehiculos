using System.Security.Claims;
using Alquiler_Vehiculos.Controllers.Usuario.User;
using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Errors.Auth;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Usuarios;
using Alquiler_Vehiculos.Services.Usuarios.Auth;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Controllers;

[TestFixture]
[Category("Unit")]
public class AuthControllerTests {
    private Mock<IAuthService> _service = null!;
    private AuthController _controller = null!;

    [SetUp]
    public void SetUp() {
        _service = new Mock<IAuthService>();
        _controller = new AuthController(_service.Object) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Test]
    public async Task Login_CredencialesValidas_DevuelveTokenCon200() {
        // Arrange
        var dto = new LoginDto { Email = "cliente@example.com", Password = "ejemplo" };
        var respuesta = new AuthResponseDto("token-de-prueba", dto.Email, "Cliente", new(2026, 12, 1));
        _service.Setup(s => s.Login(dto)).ReturnsAsync(Result.Success<AuthResponseDto, DomainError>(respuesta));

        // Act
        var resultado = await _controller.Login(dto);

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(respuesta);
        _service.Verify(s => s.Login(dto), Times.Once);
        _service.Verify(s => s.SignUpAsync(It.IsAny<SignupDto>()), Times.Never);
    }

    [Test]
    public async Task Login_CredencialesInvalidas_Devuelve401() {
        // Arrange
        var dto = new LoginDto();
        _service.Setup(s => s.Login(dto)).ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(AuthError.CredencialesInvalidas()));

        // Act
        var resultado = await _controller.Login(dto);

        // Assert
        var respuesta = resultado.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        respuesta.Value.Should().BeEquivalentTo(new { code = "CREDENCIALES_INVALIDAS", message = "Email o contraseña incorrectos" });
        _service.Verify(s => s.Login(dto), Times.Once);
        _service.Verify(s => s.SignUpAsync(It.IsAny<SignupDto>()), Times.Never);
    }

    [Test]
    public async Task Signup_AltaCorrecta_DevuelveTokenCon200() {
        // Arrange
        var dto = new SignupDto { Email = "cliente@example.com" };
        var respuesta = new AuthResponseDto("token-de-prueba", dto.Email, "Cliente", new(2026, 12, 1));
        _service.Setup(s => s.SignUpAsync(dto)).ReturnsAsync(Result.Success<AuthResponseDto, DomainError>(respuesta));

        // Act
        var resultado = await _controller.Signup(dto);

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(respuesta);
        _service.Verify(s => s.SignUpAsync(dto), Times.Once);
        _service.Verify(s => s.Login(It.IsAny<LoginDto>()), Times.Never);
    }

    [Test]
    public async Task Signup_EmailDuplicado_Devuelve409() {
        // Arrange
        var dto = new SignupDto { Email = "cliente@example.com" };
        var error = AuthError.EmailExistente(dto.Email);
        _service.Setup(s => s.SignUpAsync(dto)).ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(error));

        // Act
        var resultado = await _controller.Signup(dto);

        // Assert
        resultado.Result.Should().BeOfType<ConflictObjectResult>().Which.Value
            .Should().BeEquivalentTo(new { code = error.Code, message = error.Message });
        _service.Verify(s => s.SignUpAsync(dto), Times.Once);
        _service.Verify(s => s.Login(It.IsAny<LoginDto>()), Times.Never);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("identificador-invalido")]
    public async Task GetMe_SinIdentificadorValido_NoConsultaServicioYDevuelve401(string? identificador) {
        // Arrange
        var claims = identificador is null ? Array.Empty<Claim>() : new[] { new Claim(ClaimTypes.NameIdentifier, identificador) };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "prueba"));

        // Act
        var resultado = await _controller.GetMe();

        // Assert
        var respuesta = resultado.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        respuesta.Value.Should().BeOfType<ApiErrorResponse>().Which.Message.Should().Be("Token inválido o usuario no identificado");
        _service.Verify(s => s.GetMeAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task GetMe_UsuarioExistente_DevuelvePerfilCon200() {
        // Arrange
        var id = Guid.NewGuid();
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], "prueba"));
        var perfil = new UserDto(id, "cliente@example.com", "Lucia", "Fuertes", "Cliente");
        _service.Setup(s => s.GetMeAsync(id)).ReturnsAsync(Result.Success<UserDto, DomainError>(perfil));

        // Act
        var resultado = await _controller.GetMe();

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(perfil);
        _service.Verify(s => s.GetMeAsync(id), Times.Once);
    }

    [Test]
    public async Task GetMe_UsuarioInexistente_Devuelve404() {
        // Arrange
        var id = Guid.NewGuid();
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], "prueba"));
        var error = UsuarioError.NotFound(id);
        _service.Setup(s => s.GetMeAsync(id)).ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        // Act
        var resultado = await _controller.GetMe();

        // Assert
        resultado.Result.Should().BeOfType<NotFoundObjectResult>().Which.Value
            .Should().BeEquivalentTo(new { code = error.Code, message = error.Message });
        _service.Verify(s => s.GetMeAsync(id), Times.Once);
    }
}
