using Alquiler_Vehiculos.Errors.Storage;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Alquiler_Vehiculos.Storage.Tests;

[TestFixture]
[Category("Integration")]
public class StorageServiceTests : StorageTestBase {
    private static FormFile Archivo(Stream contenido, string nombre = "coche.jpg", string tipo = "image/jpeg") =>
        new(contenido, 0, contenido.Length, "file", nombre) { Headers = new HeaderDictionary(), ContentType = tipo };

    [TestCase(false)]
    [TestCase(true)]
    public void Constructor_RaizDelEntorno_CreaUploadsSinBorrarContenido(bool webRoot) {
        // Arrange
        var raiz = webRoot ? Path.Combine(RootPath, "wwwroot") : RootPath;
        var configuracion = Configuracion();

        // Act
        var servicio = CrearServicio(configuracion, webRoot ? raiz : null);

        // Assert
        Directory.Exists(Path.Combine(raiz, "uploads")).Should().BeTrue();
        servicio.GetFullPath("coche.jpg").Should().Be(Path.Combine(raiz, "uploads", "coche.jpg"));
    }

    [Test]
    public void Constructor_UploadsExistente_ConservaArchivos() {
        // Arrange
        Directory.CreateDirectory(Path.Combine(RootPath, "uploads"));
        var archivo = Path.Combine(RootPath, "uploads", "existente.jpg");
        File.WriteAllText(archivo, "conservar");

        // Act
        CrearServicio();

        // Assert
        File.ReadAllText(archivo).Should().Be("conservar");
    }

    [TestCase("coche.jpg", "image/jpeg", ".jpg")]
    [TestCase("mi-coche.JPG", "IMAGE/JPEG", ".jpg")]
    [TestCase("mi coche.png", "image/png", ".png")]
    [TestCase("coche.gif", "image/gif", ".gif")]
    public async Task SaveFileAsync_ArchivoValido_GuardaContenidoYDevuelveFoto(string nombre, string tipo, string extension) {
        // Arrange
        var servicio = CrearServicio();
        byte[] datos = [1, 2, 3, 4];
        using var contenido = new MemoryStream(datos);
        var archivo = Archivo(contenido, nombre, tipo);
        var antes = DateTime.UtcNow;

        // Act
        var resultado = await servicio.SaveFileAsync(archivo, "vehiculos");

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        var foto = resultado.Value;
        foto.Nombre.Should().MatchRegex(@"^\d{14}_[a-f0-9]{8}_[A-Za-z_]+\.(jpg|png|gif)$");
        foto.Extension.Should().Be(extension);
        foto.Tamano.Should().Be(datos.Length);
        foto.Ruta.Should().Be($"/uploads/vehiculos/{foto.Nombre}");
        foto.Activo.Should().BeTrue();
        foto.CreatedAt.Should().BeOnOrAfter(antes).And.BeOnOrBefore(DateTime.UtcNow);
        foto.UpdatedAt.Should().BeNull();
        foto.DeletedAt.Should().BeNull();
        File.ReadAllBytes(Path.Combine(RootPath, "uploads", "vehiculos", foto.Nombre)).Should().Equal(datos);
    }

    [TestCase("coche.pdf", "image/jpeg", "extension")]
    [TestCase("coche.jpg", "application/pdf", "contenido")]
    [TestCase("coche..jpg", "image/jpeg", "nombre")]
    [TestCase("coche\\foto.jpg", "image/jpeg", "nombre")]
    public async Task SaveFileAsync_ArchivoInvalido_DevuelveErrorSinGuardar(string nombre, string tipo, string caso) {
        // Arrange
        var servicio = CrearServicio();
        using var contenido = new MemoryStream(new byte[] { 1 });
        var archivo = Archivo(contenido, nombre, tipo);
        var esperado = caso switch {
            "extension" => StorageError.ExtensionNoPermitida(),
            "contenido" => StorageError.TipoContenidoNoPermitido(),
            _ => StorageError.NombreArchivoInvalido()
        };

        // Act
        var resultado = await servicio.SaveFileAsync(archivo, "vehiculos");

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(esperado);
        Directory.GetFileSystemEntries(Path.Combine(RootPath, "uploads")).Should().BeEmpty();
    }

    [Test]
    public async Task SaveFileAsync_TipoContenidoNulo_DevuelveErrorSinCopiar() {
        // Arrange
        var servicio = CrearServicio();
        var archivo = new Mock<IFormFile>();
        archivo.Setup(f => f.Length).Returns(1);
        archivo.Setup(f => f.FileName).Returns("coche.jpg");
        archivo.Setup(f => f.ContentType).Returns((string)null!);

        // Act
        var resultado = await servicio.SaveFileAsync(archivo.Object, "vehiculos");

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(StorageError.TipoContenidoNoPermitido());
        archivo.VerifyGet(f => f.Length, Times.Once);
        archivo.VerifyGet(f => f.FileName, Times.Once);
        archivo.VerifyGet(f => f.ContentType, Times.Once);
        archivo.Verify(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
        Directory.GetFileSystemEntries(Path.Combine(RootPath, "uploads")).Should().BeEmpty();
    }

    [TestCase(4, true)]
    [TestCase(5, true)]
    [TestCase(6, false)]
    public async Task SaveFileAsync_TamanoConfigurado_RespetaLimite(int tamano, bool valido) {
        // Arrange
        var servicio = CrearServicio(Configuracion(new() { ["Storage:MaxFileSize"] = "5" }));
        using var contenido = new MemoryStream(new byte[tamano]);
        var archivo = Archivo(contenido);

        // Act
        var resultado = await servicio.SaveFileAsync(archivo, "vehiculos");

        // Assert
        resultado.IsSuccess.Should().Be(valido);
        if (valido) resultado.Value.Tamano.Should().Be(tamano);
        else resultado.Error.Should().Be(StorageError.ArchivoMuyGrande());
    }

    [TestCase("coche.webp", "image/webp", true)]
    [TestCase("coche.jpg", "image/jpeg", false)]
    public async Task SaveFileAsync_ExtensionesConfiguradas_UsaConfiguracion(string nombre, string tipo, bool valido) {
        // Arrange
        var servicio = CrearServicio(Configuracion(new() {
            ["Storage:UploadPath"] = "imagenes", ["Storage:AllowedExtensions:0"] = ".webp",
            ["Storage:AllowedContentTypes:0"] = "image/webp"
        }));
        using var contenido = new MemoryStream(new byte[] { 1 });
        var archivo = Archivo(contenido, nombre, tipo);

        // Act
        var resultado = await servicio.SaveFileAsync(archivo, "vehiculos");

        // Assert
        resultado.IsSuccess.Should().Be(valido);
        if (valido) resultado.Value.Ruta.Should().StartWith("/imagenes/vehiculos/");
        else resultado.Error.Should().Be(StorageError.ExtensionNoPermitida());
    }

    [Test]
    public async Task SaveFileAsync_CarpetaOcupadaPorArchivo_DevuelveErrorInterno() {
        // Arrange
        var servicio = CrearServicio();
        File.WriteAllText(Path.Combine(RootPath, "uploads", "vehiculos"), "bloqueo");
        using var contenido = new MemoryStream(new byte[] { 1 });
        var archivo = Archivo(contenido);

        // Act
        var resultado = await servicio.SaveFileAsync(archivo, "vehiculos");

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(StorageError.ErrorGuardando());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("inexistente.jpg")]
    public async Task DeleteFileAsync_SinArchivo_DevuelveExito(string? nombre) {
        // Arrange
        var servicio = CrearServicio();

        // Act
        var resultado = await servicio.DeleteFileAsync(nombre!);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeTrue();
    }

    [Test]
    public async Task DeleteFileAsync_ArchivoExistente_EliminaDelDisco() {
        // Arrange
        var servicio = CrearServicio();
        var ruta = Path.Combine(RootPath, "uploads", "borrar.jpg");
        File.WriteAllText(ruta, "foto");

        // Act
        var resultado = await servicio.DeleteFileAsync("borrar.jpg");

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeTrue();
        File.Exists(ruta).Should().BeFalse();
    }

    [Test]
    public async Task DeleteFileAsync_DirectorioSinPermisoDeEscritura_DevuelveErrorInterno() {
        // Arrange
        var servicio = CrearServicio();
        var directorio = Path.Combine(RootPath, "uploads");
        var ruta = Path.Combine(directorio, "conservar.jpg");
        File.WriteAllText(ruta, "foto");
        if (!OperatingSystem.IsLinux()) throw new InvalidOperationException("Este caso requiere el contenedor Linux.");
        File.SetUnixFileMode(directorio, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try {
            // Act
            var resultado = await servicio.DeleteFileAsync("conservar.jpg");

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().Be(StorageError.ErrorEliminando());
            File.Exists(ruta).Should().BeTrue();
        } finally {
            File.SetUnixFileMode(directorio, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase("inexistente.jpg", false)]
    [TestCase("existente.jpg", true)]
    public void FileExists_NombreRecibido_CompruebaArchivoReal(string? nombre, bool esperado) {
        // Arrange
        var servicio = CrearServicio();
        File.WriteAllText(Path.Combine(RootPath, "uploads", "existente.jpg"), "foto");

        // Act
        var existe = servicio.FileExists(nombre!);

        // Assert
        existe.Should().Be(esperado);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void GetFullPath_RutaAbsolutaORelativa_ResuelveSegunContrato(bool absoluta) {
        // Arrange
        var servicio = CrearServicio();
        var entrada = absoluta ? Path.Combine(RootPath, "coche.jpg") : "coche.jpg";
        var esperado = absoluta ? entrada : Path.Combine(RootPath, "uploads", entrada);

        // Act
        var ruta = servicio.GetFullPath(entrada);

        // Assert
        ruta.Should().Be(esperado);
    }

    [TestCase("/uploads/vehiculos/coche.jpg")]
    [TestCase("/UPLOADS/vehiculos/coche.jpg")]
    public void GetFullPath_PrefijoUploadsEnraizado_ConservaRutaPorRetornoPrevio(string entrada) {
        // Arrange
        var servicio = CrearServicio();

        // Act
        var ruta = servicio.GetFullPath(entrada);

        // Assert
        Path.IsPathRooted(entrada).Should().BeTrue();
        ruta.Should().Be(entrada);
    }

    [TestCase(null, "/uploads/vehiculos/coche.jpg")]
    [TestCase("marcas", "/uploads/marcas/coche.jpg")]
    public void GetRelativePath_CarpetaIndicadaOPorDefecto_DevuelveReferencia(string? carpeta, string esperado) {
        // Arrange
        var servicio = CrearServicio();

        // Act
        var ruta = carpeta is null ? servicio.GetRelativePath("coche.jpg") : servicio.GetRelativePath("coche.jpg", carpeta);

        // Assert
        ruta.Should().Be(esperado);
    }

    [TestCase(true, 0, true)]
    [TestCase(false, 0, true)]
    [TestCase(false, 1, false)]
    public void IsEmpty_ArchivoNuloVacioOConDatos_DevuelveResultadoEsperado(bool nulo, int tamano, bool esperado) {
        // Arrange
        var servicio = CrearServicio();
        using var contenido = new MemoryStream(new byte[tamano]);
        var archivo = nulo ? null : Archivo(contenido);

        // Act
        var vacio = servicio.IsEmpty(archivo);

        // Assert
        vacio.Should().Be(esperado);
    }
}
