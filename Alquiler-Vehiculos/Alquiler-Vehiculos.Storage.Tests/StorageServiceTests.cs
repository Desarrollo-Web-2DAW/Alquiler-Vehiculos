using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Errors.Storage;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Alquiler_Vehiculos.Storage.Tests;

[TestFixture]
[Category("Integration")]
public partial class StorageServiceTests : StorageTestBase {
    private static FormFile Archivo(Stream contenido, string nombre = "coche.jpg", string tipo = "image/jpeg") =>
        new(contenido, 0, contenido.Length, "file", nombre) {
            Headers = new HeaderDictionary(), ContentType = tipo
        };

    [Test]
    public void Constructor_RaizConfigurada_CreaUploadsSinBorrarContenido() {
        var servicio = CrearServicio();

        Directory.Exists(Path.Combine(RootPath, "uploads")).Should().BeTrue();
        servicio.GetFullPath("coche.jpg").Should().Be(Path.Combine(RootPath, "uploads", "coche.jpg"));
    }

    [Test]
    public void Constructor_UploadsExistente_ConservaArchivos() {
        Directory.CreateDirectory(Path.Combine(RootPath, "uploads"));
        var archivo = Path.Combine(RootPath, "uploads", "existente.jpg");
        File.WriteAllText(archivo, "conservar");

        CrearServicio();

        File.ReadAllText(archivo).Should().Be("conservar");
    }

    [TestCase("coche.jpg", ".jpg")]
    [TestCase("mi-coche.JPG", ".jpg")]
    [TestCase("mi coche.png", ".png")]
    [TestCase("coche.gif", ".gif")]
    public async Task SaveFileAsync_ArchivoValido_GuardaBytesYDevuelveMetadatos(string original, string extension) {
        var servicio = CrearServicio();
        var nombre = $"{Guid.NewGuid():N}{extension}";
        byte[] datos = [1, 2, 3, 4];
        using var contenido = new MemoryStream(datos);
        var archivo = Archivo(contenido, original);

        var resultado = await servicio.SaveFileAsync(archivo, "vehiculos", nombre);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Nombre.Should().Be(nombre);
        resultado.Value.Extension.Should().Be(extension);
        resultado.Value.Tamano.Should().Be(datos.Length);
        resultado.Value.Ruta.Should().Be($"/uploads/vehiculos/{nombre}");
        File.ReadAllBytes(Path.Combine(RootPath, "uploads", "vehiculos", nombre)).Should().Equal(datos);
        await using var lectura = CrearContextoDeLectura();
        (await lectura.Fotos.CountAsync()).Should().Be(0);
    }

    [Test]
    public async Task SaveFileAsync_FallaLaCopia_LimpiaElArchivoParcial() {
        var servicio = CrearServicio();
        var archivo = new Mock<IFormFile>();
        archivo.SetupGet(f => f.Length).Returns(1);
        archivo.SetupGet(f => f.FileName).Returns("coche.jpg");
        archivo.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Copia interrumpida"));

        var resultado = await servicio.SaveFileAsync(archivo.Object, "vehiculos", "fallo.jpg");

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(StorageError.ErrorGuardando());
        File.Exists(Path.Combine(RootPath, "uploads", "vehiculos", "fallo.jpg")).Should().BeFalse();
    }

    [Test]
    public async Task SaveFileAsync_CarpetaOcupadaPorArchivo_DevuelveErrorInterno() {
        var servicio = CrearServicio();
        File.WriteAllText(Path.Combine(RootPath, "uploads", "vehiculos"), "bloqueo");
        using var contenido = new MemoryStream([1]);

        var resultado = await servicio.SaveFileAsync(Archivo(contenido), "vehiculos", "coche.jpg");

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(StorageError.ErrorGuardando());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("inexistente.jpg")]
    public async Task DeleteFileAsync_SinArchivo_DevuelveExito(string? nombre) {
        var resultado = await CrearServicio().DeleteFileAsync(nombre!);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeTrue();
    }

    [Test]
    public async Task DeleteFileAsync_ArchivoExistente_EliminaSoloElArchivo() {
        var servicio = CrearServicio();
        var ruta = Path.Combine(RootPath, "uploads", "borrar.jpg");
        File.WriteAllText(ruta, "foto");
        Db.Fotos.Add(new Foto {
            Nombre = "borrar.jpg", Ruta = "/uploads/borrar.jpg", Extension = ".jpg", Tamano = 4
        });
        await Db.SaveChangesAsync();

        var resultado = await servicio.DeleteFileAsync("borrar.jpg");

        resultado.IsSuccess.Should().BeTrue();
        File.Exists(ruta).Should().BeFalse();
        await using var lectura = CrearContextoDeLectura();
        var guardada = await lectura.Fotos.SingleAsync(f => f.Nombre == "borrar.jpg");
        guardada.Activo.Should().BeTrue();
        guardada.DeletedAt.Should().BeNull();
    }

    [Test]
    public async Task DeleteFileAsync_DirectorioSinPermisoDeEscritura_DevuelveErrorInterno() {
        var servicio = CrearServicio();
        var directorio = Path.Combine(RootPath, "uploads");
        var ruta = Path.Combine(directorio, "conservar.jpg");
        File.WriteAllText(ruta, "foto");
        if (!OperatingSystem.IsLinux()) throw new InvalidOperationException("Este caso requiere el contenedor Linux.");
        File.SetUnixFileMode(directorio, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try {
            var resultado = await servicio.DeleteFileAsync("conservar.jpg");

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
        var servicio = CrearServicio();
        File.WriteAllText(Path.Combine(RootPath, "uploads", "existente.jpg"), "foto");

        servicio.FileExists(nombre!).Should().Be(esperado);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void GetFullPath_RutaAbsolutaORelativa_ResuelveSegunContrato(bool absoluta) {
        var servicio = CrearServicio();
        var entrada = absoluta ? Path.Combine(RootPath, "coche.jpg") : "coche.jpg";
        var esperado = absoluta ? entrada : Path.Combine(RootPath, "uploads", entrada);

        servicio.GetFullPath(entrada).Should().Be(esperado);
    }

    [TestCase("/uploads/vehiculos/coche.jpg")]
    [TestCase("/UPLOADS/vehiculos/coche.jpg")]
    public void GetFullPath_RutaEnraizada_SeDevuelveSinCambios(string entrada) {
        var servicio = CrearServicio();

        servicio.GetFullPath(entrada).Should().Be(entrada);
    }

    [TestCase(null, "/uploads/vehiculos/coche.jpg")]
    [TestCase("marcas", "/uploads/marcas/coche.jpg")]
    public void GetRelativePath_CarpetaIndicadaOPorDefecto_DevuelveReferencia(string? carpeta, string esperado) {
        var servicio = CrearServicio();

        var ruta = carpeta is null
            ? servicio.GetRelativePath("coche.jpg")
            : servicio.GetRelativePath("coche.jpg", carpeta);

        ruta.Should().Be(esperado);
    }

    [TestCase(true, 0, true)]
    [TestCase(false, 0, true)]
    [TestCase(false, 1, false)]
    public void IsEmpty_ArchivoNuloVacioOConDatos_DevuelveResultadoEsperado(bool nulo, int tamano, bool esperado) {
        var servicio = CrearServicio();
        using var contenido = new MemoryStream(new byte[tamano]);
        var archivo = nulo ? null : Archivo(contenido);

        servicio.IsEmpty(archivo).Should().Be(esperado);
    }
}
