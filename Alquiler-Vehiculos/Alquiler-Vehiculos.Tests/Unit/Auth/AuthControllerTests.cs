using System.Security.Claims;
using Alquiler_Vehiculos.Controllers.Usuario.User;
using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Errors.Auth;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Services.Usuarios.Auth;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Auth;

[TestFixture]
[Category("Unit")]
public class AuthControllerTests {
    private Mock<IAuthService> _authServiceMock = null!;
    private Mock<ILogger<AuthController>> _loggerMock = null!;
    private AuthController _controller = null!;

    [SetUp]
    public void SetUp() {
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_authServiceMock.Object, _loggerMock.Object);
    }

    #region Login

    [Test]
    public async Task Login_CredencialesValidas_Devuelve200ConToken() {
        // Arrange
        var dto = new LoginDto { Email = "ana@alquiler.local", Password = "Password123!" };
        var expected = new AuthResponseDto("jwt-token", dto.Email, "Cliente", DateTime.UtcNow.AddMinutes(30));

        _authServiceMock
            .Setup(s => s.LoginAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AuthResponseDto, DomainError>(expected));

        // Act
        var result = await _controller.Login(dto, CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().Be(expected);
    }

    [Test]
    public async Task Login_CredencialesInvalidas_Devuelve401() {
        // Arrange
        var dto = new LoginDto { Email = "ana@alquiler.local", Password = "MalPassword1!" };
        _authServiceMock
            .Setup(s => s.LoginAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(AuthError.CredencialesInvalidas()));

        // Act
        var result = await _controller.Login(dto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Test]
    public async Task Login_CuentaInactiva_Devuelve403() {
        // Arrange
        var dto = new LoginDto { Email = "baja@alquiler.local", Password = "Password123!" };
        _authServiceMock
            .Setup(s => s.LoginAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(AuthError.CuentaInactiva()));

        // Act
        var result = await _controller.Login(dto, CancellationToken.None);

        // Assert
        var obj = result.Result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    #endregion

    #region Signup

    [Test]
    public async Task Signup_DatosValidos_Devuelve201Created() {
        // Arrange
        var dto = new SignupDto {
            Email = "nuevo@alquiler.local",
            Password = "Password123!",
            Nombre = "Ana",
            Apellidos = "García",
            Telefono = "600112233",
            FechaNacimiento = new DateOnly(1998, 5, 10),
            Dni = "12345678Z"
        };
        var expected = new AuthResponseDto("jwt-token", dto.Email, "Cliente", DateTime.UtcNow.AddMinutes(30));

        _authServiceMock
            .Setup(s => s.SignUpAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AuthResponseDto, DomainError>(expected));

        // Act
        var result = await _controller.Signup(dto, CancellationToken.None);

        // Assert
        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.Value.Should().Be(expected);
    }

    [Test]
    public async Task Signup_EmailDuplicado_Devuelve409Conflict() {
        // Arrange
        var dto = new SignupDto { Email = "repetido@alquiler.local" };
        _authServiceMock
            .Setup(s => s.SignUpAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(AuthError.EmailExistente(dto.Email)));

        // Act
        var result = await _controller.Signup(dto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    #endregion

    #region GetMe

    [Test]
    public async Task GetMe_SinClaimNameIdentifier_Devuelve401() {
        // Arrange
        _controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

        // Act
        var result = await _controller.GetMe(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Test]
    public async Task GetMe_UsuarioAutenticado_Devuelve200ConPerfil() {
        // Arrange
        var userId = Guid.NewGuid();
        var expected = new UserDto(userId, "ana@alquiler.local", "Ana", "García", "Cliente");
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Bearer");

        _controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _authServiceMock
            .Setup(s => s.GetMeAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(expected));

        // Act
        var result = await _controller.GetMe(CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(expected);
    }

    #endregion
}
