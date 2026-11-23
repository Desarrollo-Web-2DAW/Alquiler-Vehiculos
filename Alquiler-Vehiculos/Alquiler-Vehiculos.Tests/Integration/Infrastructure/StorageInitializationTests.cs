using Alquiler_Vehiculos.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Alquiler_Vehiculos.Tests.Integration.Infrastructure;

[TestFixture]
[Category("Integration")]
public class StorageInitializationTests {
    private string _raiz = null!;

    [SetUp]
    public void CrearRaizTemporal() {
        _raiz = Path.Combine(Path.GetTempPath(), "storage-init-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_raiz);
    }

    [TearDown]
    public void EliminarRaizTemporal() {
        if (Directory.Exists(_raiz)) Directory.Delete(_raiz, true);
    }

    private WebApplication CrearAplicacion(Dictionary<string, string?>? valores = null) {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            ContentRootPath = _raiz, WebRootPath = _raiz, EnvironmentName = "Testing"
        });
        builder.Configuration.AddInMemoryCollection(valores ?? new Dictionary<string, string?> {
            ["Storage:UploadPath"] = "uploads",
            ["Storage:Folders:Vehiculos"] = "vehiculos"
        });
        return builder.Build();
    }

    private static void PrepararSemilla(string raiz, string rutaRelativa, string contenido) {
        var ruta = Path.Combine(raiz, "SeedImages", rutaRelativa);
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        File.WriteAllText(ruta, contenido);
    }

    [Test]
    public async Task InitializeStorage_SinDesarrollo_CreaUploadsSinCopiarSemillas() {
        // Arrange
        PrepararSemilla(_raiz, Path.Combine("vehiculos", "seed-1234BCD.jpg"), "semilla");
        await using var app = CrearAplicacion();

        // Act
        app.InitializeStorage(app.Configuration, false);

        // Assert
        var uploads = Path.Combine(_raiz, "uploads");
        Directory.Exists(uploads).Should().BeTrue();
        File.ReadAllText(Path.Combine(uploads, ".gitkeep")).Should().BeEmpty();
        Directory.Exists(Path.Combine(uploads, "vehiculos")).Should().BeFalse();
    }

    [Test]
    public async Task InitializeStorage_Desarrollo_ConservaExistentesYCopiaFaltantes() {
        // Arrange
        PrepararSemilla(_raiz, Path.Combine("vehiculos", "seed-1234BCD.jpg"), "nueva");
        PrepararSemilla(_raiz, Path.Combine("vehiculos", "sub", "seed-2345CDF.jpg"), "anidada");
        var destino = Path.Combine(_raiz, "uploads", "vehiculos");
        Directory.CreateDirectory(destino);
        File.WriteAllText(Path.Combine(destino, "seed-1234BCD.jpg"), "propia");
        await using var app = CrearAplicacion();

        // Act
        app.InitializeStorage(app.Configuration, true);

        // Assert
        File.ReadAllText(Path.Combine(destino, "seed-1234BCD.jpg")).Should().Be("propia");
        File.ReadAllText(Path.Combine(destino, "sub", "seed-2345CDF.jpg")).Should().Be("anidada");
        File.ReadAllText(Path.Combine(destino, ".gitkeep")).Should().BeEmpty();
    }

    [Test]
    public async Task InitializeStorage_SinSeedImages_OmiteLaCopia() {
        // Arrange
        await using var app = CrearAplicacion();

        // Act
        app.InitializeStorage(app.Configuration, true);

        // Assert
        var uploads = Path.Combine(_raiz, "uploads");
        Directory.Exists(uploads).Should().BeTrue();
        Directory.Exists(Path.Combine(uploads, "vehiculos")).Should().BeFalse();
    }

    [Test]
    public async Task InitializeStorage_SinSemillaParaUnDominio_PreparaElResto() {
        // Arrange
        PrepararSemilla(_raiz, Path.Combine("vehiculos", "seed-1234BCD.jpg"), "semilla");
        await using var app = CrearAplicacion(new Dictionary<string, string?> {
            ["Storage:UploadPath"] = "uploads",
            ["Storage:Folders:Vehiculos"] = "vehiculos",
            ["Storage:Folders:Marcas"] = "marcas"
        });

        // Act
        app.InitializeStorage(app.Configuration, true);

        // Assert
        var uploads = Path.Combine(_raiz, "uploads");
        File.Exists(Path.Combine(uploads, "vehiculos", "seed-1234BCD.jpg")).Should().BeTrue();
        File.Exists(Path.Combine(uploads, "marcas", ".gitkeep")).Should().BeTrue();
        Directory.GetFiles(Path.Combine(uploads, "marcas")).Should().ContainSingle();
    }

    [Test]
    public async Task InitializeStorage_ErrorAlCrearLaCarpeta_NoPropagaLaExcepcion() {
        // Arrange
        File.WriteAllText(Path.Combine(_raiz, "uploads"), "bloqueo");
        await using var app = CrearAplicacion();

        // Act
        Action act = () => app.InitializeStorage(app.Configuration, true);

        // Assert
        act.Should().NotThrow();
        File.ReadAllText(Path.Combine(_raiz, "uploads")).Should().Be("bloqueo");
    }
}
