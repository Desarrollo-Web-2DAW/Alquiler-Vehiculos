using Alquiler_Vehiculos.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Alquiler_Vehiculos.Tests.Unit.Middleware;

[TestFixture]
[Category("Unit")]
public class GlobalExceptionHandlerExtensionsTests {
    [Test]
    public async Task UseGlobalExceptionHandler_ExcepcionEnPipeline_RegistraMiddlewareYResponde500() {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<GlobalExceptionHandler>>(NullLogger<GlobalExceptionHandler>.Instance);
        using var provider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(provider);
        var context = new DefaultHttpContext { RequestServices = provider };
        using var cuerpo = new MemoryStream();
        context.Response.Body = cuerpo;

        // Act
        var registrado = app.UseGlobalExceptionHandler();
        app.Run(_ => throw new InvalidOperationException("Detalle privado"));
        await app.Build()(context);

        // Assert
        registrado.Should().BeSameAs(app);
        context.Response.StatusCode.Should().Be(500);
        cuerpo.Position = 0;
        using var reader = new StreamReader(cuerpo);
        var json = await reader.ReadToEndAsync();
        json.Should().Contain("Ha ocurrido un error interno").And.NotContain("Detalle privado");
    }
}
