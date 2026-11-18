using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Services.Storage;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Alquiler_Vehiculos.Tests.Unit.Infrastructure;

[TestFixture]
[Category("Unit")]
public class StorageConfigTests {
    [Test]
    public void AddStorage_ServiciosVacios_RegistraServicioScopedYDevuelveColeccion() {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var resultado = services.AddStorage(configuration);

        // Assert
        resultado.Should().BeSameAs(services);
        services.Should().ContainSingle(s => s.ServiceType == typeof(IStorageService)
            && s.ImplementationType == typeof(StorageService) && s.Lifetime == ServiceLifetime.Scoped);
    }

    [Test]
    public void AddStorage_SeccionStorage_MapeaOpcionesYCarpetas() {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Storage:UploadPath"] = "imagenes", ["Storage:MaxFileSize"] = "1024",
            ["Storage:AllowedExtensions"] = ".png", ["Storage:AllowedContentTypes"] = "image/png",
            ["Storage:DefaultImage"] = "default.png", ["Storage:Folders:Vehiculos"] = "coches"
        }).Build();

        // Act
        services.AddStorage(configuration);
        using var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<IOptions<StorageSettings>>().Value;

        // Assert
        settings.UploadPath.Should().Be("imagenes");
        settings.MaxFileSize.Should().Be("1024");
        settings.AllowedExtensions.Should().Be(".png");
        settings.AllowedContentTypes.Should().Be("image/png");
        settings.DefaultImage.Should().Be("default.png");
        settings.Folders["vehiculos"].Should().Be("coches");
    }
}
