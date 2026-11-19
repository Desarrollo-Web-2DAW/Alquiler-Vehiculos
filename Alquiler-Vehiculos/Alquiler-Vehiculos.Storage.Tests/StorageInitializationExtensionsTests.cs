using Alquiler_Vehiculos.Infrastructure;
using FluentAssertions;

namespace Alquiler_Vehiculos.Storage.Tests;

[TestFixture]
[Category("Integration")]
public class StorageInitializationExtensionsTests : StorageTestBase {
    [TestCase(false)]
    [TestCase(true)]
    public async Task InitializeStorage_SinUploads_CreaCarpetaYGitkeep(bool webRoot) {
        // Arrange
        var raiz = webRoot ? Path.Combine(RootPath, "wwwroot") : RootPath;
        Directory.CreateDirectory(raiz);
        await using var app = CrearAplicacion(webRoot ? raiz : null);
        var configuracion = Configuracion();

        // Act
        app.InitializeStorage(configuracion);

        // Assert
        Directory.Exists(Path.Combine(raiz, "uploads")).Should().BeTrue();
        File.ReadAllText(Path.Combine(raiz, "uploads", ".gitkeep")).Should().BeEmpty();
    }

    [Test]
    public async Task InitializeStorage_UploadsExistente_LimpiaContenidoYConservaGitkeep() {
        // Arrange
        await using var app = CrearAplicacion();
        var uploads = Path.Combine(RootPath, "uploads");
        Directory.CreateDirectory(Path.Combine(uploads, "viejas", "anidadas"));
        File.WriteAllText(Path.Combine(uploads, ".gitkeep"), "conservar");
        File.WriteAllText(Path.Combine(uploads, "vieja.jpg"), "borrar");
        File.WriteAllText(Path.Combine(uploads, "viejas", "anidadas", "foto.jpg"), "borrar");
        var configuracion = Configuracion();

        // Act
        app.InitializeStorage(configuracion);

        // Assert
        Directory.GetFiles(uploads).Should().Equal(Path.Combine(uploads, ".gitkeep"));
        File.ReadAllText(Path.Combine(uploads, ".gitkeep")).Should().Be("conservar");
        Directory.GetDirectories(uploads).Should().BeEmpty();
    }

    [Test]
    public async Task InitializeStorage_SeedImages_CopiaRecursivamenteYPreparaCarpetasSinSemillas() {
        // Arrange
        await using var app = CrearAplicacion();
        var origen = Path.Combine(RootPath, "SeedImages", "vehiculos");
        Directory.CreateDirectory(Path.Combine(origen, "miniaturas"));
        File.WriteAllText(Path.Combine(origen, "coche.jpg"), "imagen");
        File.WriteAllText(Path.Combine(origen, "miniaturas", "coche.jpg"), "miniatura");
        var configuracion = Configuracion(new() {
            ["Storage:Folders:Vehiculos"] = "vehiculos", ["Storage:Folders:Marcas"] = "marcas"
        });

        // Act
        app.InitializeStorage(configuracion);

        // Assert
        var destino = Path.Combine(RootPath, "uploads", "vehiculos");
        File.ReadAllText(Path.Combine(destino, "coche.jpg")).Should().Be("imagen");
        File.ReadAllText(Path.Combine(destino, "miniaturas", "coche.jpg")).Should().Be("miniatura");
        File.Exists(Path.Combine(destino, ".gitkeep")).Should().BeTrue();
        File.Exists(Path.Combine(RootPath, "uploads", "marcas", ".gitkeep")).Should().BeTrue();
        File.ReadAllText(Path.Combine(origen, "coche.jpg")).Should().Be("imagen");
    }

    [Test]
    public async Task InitializeStorage_SeedImagesSinConfiguracion_NoCopiaArchivos() {
        // Arrange
        await using var app = CrearAplicacion();
        Directory.CreateDirectory(Path.Combine(RootPath, "SeedImages"));
        File.WriteAllText(Path.Combine(RootPath, "SeedImages", "ignorar.jpg"), "imagen");
        var configuracion = Configuracion();

        // Act
        app.InitializeStorage(configuracion);

        // Assert
        Directory.GetFileSystemEntries(Path.Combine(RootPath, "uploads"))
            .Should().Equal(Path.Combine(RootPath, "uploads", ".gitkeep"));
    }

    [Test]
    public async Task InitializeStorage_ErrorDeCreacion_NoPropagaExcepcion() {
        // Arrange
        await using var app = CrearAplicacion();
        var bloqueo = Path.Combine(RootPath, "uploads");
        File.WriteAllText(bloqueo, "conservar");
        var configuracion = Configuracion();

        // Act
        Action inicializar = () => app.InitializeStorage(configuracion);

        // Assert
        inicializar.Should().NotThrow();
        File.ReadAllText(bloqueo).Should().Be("conservar");
    }
}
