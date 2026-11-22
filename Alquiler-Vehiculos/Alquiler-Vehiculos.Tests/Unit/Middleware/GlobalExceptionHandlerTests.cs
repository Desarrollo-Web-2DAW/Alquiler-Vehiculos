using System.Text.Json;
using Alquiler_Vehiculos.Exceptions;
using Alquiler_Vehiculos.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Middleware;

/// <summary>
///     Tests unitarios para GlobalExceptionHandler.
/// </summary>
public class GlobalExceptionHandlerTests {
    private readonly GlobalExceptionHandler _handler;
    private readonly DefaultHttpContext _httpContext;
    private readonly Mock<ILogger<GlobalExceptionHandler>> _mockLogger = new();
    private readonly Mock<RequestDelegate> _mockNext = new();

    public GlobalExceptionHandlerTests() {
        _handler = new GlobalExceptionHandler(
            _mockNext.Object,
            _mockLogger.Object
        );

        _httpContext = new DefaultHttpContext();

        _httpContext.Request.Path = "/api/vehiculos";
        _httpContext.Request.Method = HttpMethods.Post;
    }

    [SetUp]
    public void Setup() {
        _httpContext.Response.Body = new MemoryStream();
    }

    [TearDown]
    public void TearDown() {
        _httpContext.Response.Body.Dispose();
    }

    private async Task<(int StatusCode, string Body)> ExecuteHandlerAsync(Exception exception) {
        _mockNext
            .Setup(next => next(It.IsAny<HttpContext>()))
            .Throws(exception);

        await _handler.InvokeAsync(_httpContext);

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(_httpContext.Response.Body);

        var body = await reader.ReadToEndAsync();

        return (_httpContext.Response.StatusCode, body);
    }

    [Test]
    public async Task InvokeAsync_ConExceptionGenerica_DeberiaRetornar500() {
        // Arrange
        var exception = new Exception("Error inesperado");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        statusCode.Should().Be(500);
        body.Should().Contain("Ha ocurrido un error interno");
    }

    [Test]
    public async Task InvokeAsync_ConBusinessException_DeberiaRetornar400() {
        // Arrange
        var exception = new BusinessException("Stock insuficiente");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        statusCode.Should().Be(400);
        body.Should().Contain("Stock insuficiente");
        body.Should().Contain("BusinessRuleError");
    }

    [Test]
    public async Task InvokeAsync_ConNotFoundException_DeberiaRetornar404() {
        // Arrange
        var exception = new NotFoundException("Vehículo no encontrado");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        var response = JsonDocument.Parse(body);
        var message = response.RootElement.GetProperty("message").GetString();
        var errorType = response.RootElement.GetProperty("errorType").GetString();

        // Assert
        statusCode.Should().Be(404);
        message.Should().Be("Vehículo no encontrado");
        errorType.Should().Be("NotFoundError");
    }

    [Test]
    public async Task InvokeAsync_ConValidationException_DeberiaRetornar400() {
        // Arrange
        var errors = new Dictionary<string, string[]> {
            { "Nombre", new[] { "El nombre es obligatorio" } },
            { "Precio", new[] { "El precio debe ser mayor a 0" } }
        };

        var exception = new ValidationException("Errores de validación", errors);

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        var response = JsonDocument.Parse(body);
        var message = response.RootElement.GetProperty("message").GetString();
        var errorType = response.RootElement.GetProperty("errorType").GetString();

        // Assert
        statusCode.Should().Be(400);
        message.Should().Be("Errores de validación");
        errorType.Should().Be("ValidationError");
    }

    [Test]
    public async Task InvokeAsync_ConValidationException_DeberiaIncluirErrors() {
        // Arrange
        var errors = new Dictionary<string, string[]> {
            { "Nombre", new[] { "El nombre es obligatorio" } },
            { "Precio", new[] { "El precio debe ser mayor a 0" } }
        };

        var exception = new ValidationException("Errores de validación", errors);

        // Act
        var (_, body) = await ExecuteHandlerAsync(exception);

        var response = JsonDocument.Parse(body);
        var validationErrors = response.RootElement.GetProperty("errors");

        // Assert
        validationErrors.GetProperty("Nombre")[0].GetString().Should().Be("El nombre es obligatorio");

        validationErrors.GetProperty("Precio")[0].GetString().Should().Be("El precio debe ser mayor a 0");
    }

    [Test]
    public async Task InvokeAsync_ConUnauthorizedAccessException_DeberiaRetornar401() {
        // Arrange
        var exception = new UnauthorizedAccessException("Token inválido");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        var response = JsonDocument.Parse(body);
        var message = response.RootElement.GetProperty("message").GetString();
        var errorType = response.RootElement.GetProperty("errorType").GetString();

        // Assert
        statusCode.Should().Be(401);
        message.Should().Be("No autorizado");
        errorType.Should().Be("UnauthorizedError");
    }

    [Test]
    public async Task InvokeAsync_ConArgumentException_DeberiaRetornar400() {
        // Arrange
        var exception = new ArgumentException("El argumento es inválido");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        var response = JsonDocument.Parse(body);
        var message = response.RootElement.GetProperty("message").GetString();
        var errorType = response.RootElement.GetProperty("errorType").GetString();

        // Assert
        statusCode.Should().Be(400);
        message.Should().Be("El argumento es inválido");
        errorType.Should().Be("ValidationError");
    }

    [Test]
    public async Task InvokeAsync_ConTimeoutException_DeberiaRetornar408() {
        // Arrange
        var exception = new TimeoutException("La operación tardó demasiado");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        var response = JsonDocument.Parse(body);
        var message = response.RootElement.GetProperty("message").GetString();
        var errorType = response.RootElement.GetProperty("errorType").GetString();

        // Assert
        statusCode.Should().Be(408);
        message.Should().Be("Tiempo de espera agotado");
        errorType.Should().Be("InternalError");
    }

    [Test]
    public async Task InvokeAsync_ConExceptionGenerica_NoDeberiaExponerDetalles() {
        // Arrange
        var exception = new NullReferenceException(
            "Object reference not set to an instance of an object."
        );

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        var response = JsonDocument.Parse(body);
        var message = response.RootElement.GetProperty("message").GetString();

        // Assert
        statusCode.Should().Be(500);
        message.Should().Be("Ha ocurrido un error interno");
        body.Should().NotContain("NullReferenceException");
        body.Should().NotContain("Object reference");
    }

    [Test]
    public async Task InvokeAsync_DeberiaRetornarContentTypeJson() {
        // Arrange
        var exception = new Exception("Error");

        // Act
        await ExecuteHandlerAsync(exception);

        // Assert
        _httpContext.Response.ContentType.Should().Contain("application/json");
    }

    [Test]
    public async Task InvokeAsync_Response_DeberiaTenerPath() {
        // Arrange
        var exception = new Exception("Error");

        // Act
        var (_, body) = await ExecuteHandlerAsync(exception);

        var response = JsonDocument.Parse(body);
        var path = response.RootElement.GetProperty("path").GetString();

        // Assert
        path.Should().Be("/api/vehiculos");
    }

    [Test]
    public async Task InvokeAsync_Response_DeberiaTenerMethod() {
        // Arrange
        var exception = new Exception("Error");

        // Act
        var (_, body) = await ExecuteHandlerAsync(exception);

        var response = JsonDocument.Parse(body);
        var method = response.RootElement.GetProperty("method").GetString();

        // Assert
        method.Should().Be("POST");
    }

    [Test]
    public async Task InvokeAsync_Response_DeberiaTenerTimestamp() {
        // Arrange
        var exception = new Exception("Error");

        // Act
        var (_, body) = await ExecuteHandlerAsync(exception);

        var response = JsonDocument.Parse(body);

        // Assert
        response.RootElement.TryGetProperty("timestamp", out var timestamp).Should().BeTrue();

        timestamp.GetString().Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task InvokeAsync_Response_DeberiaTenerErrorId() {
        // Arrange
        var exception = new Exception("Error");

        // Act
        var (_, body) = await ExecuteHandlerAsync(exception);

        var response = JsonDocument.Parse(body);
        var errorId = response.RootElement.GetProperty("errorId").GetString();

        // Assert
        errorId.Should().NotBeNullOrEmpty();
        errorId.Should().HaveLength(8);
    }

    [Test]
    public async Task InvokeAsync_SinExcepcion_NoDeberiaLanzarExcepcion() {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        _mockNext
            .Setup(next => next(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be(200);
        _mockNext.Verify(next => next(httpContext), Times.Once);

        await httpContext.Response.Body.DisposeAsync();
    }
}