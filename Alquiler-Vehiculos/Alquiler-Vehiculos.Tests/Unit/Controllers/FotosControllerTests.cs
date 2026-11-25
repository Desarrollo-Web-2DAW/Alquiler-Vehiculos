using Alquiler_Vehiculos.Controllers.Vehiculo;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Services.Storage;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Alquiler_Vehiculos.Infrastructure;

namespace Alquiler_Vehiculos.Tests.Unit.Controllers;

[TestFixture]
[Category("Unit")]
public class FotosControllerTests {
    [TestCase(nameof(FotosController.GetHistorico))]
    [TestCase(nameof(FotosController.GetArchivoHistorico))]
    public void ConsultaHistorica_SoloPersonal_NoPermiteAnonimos(string accion) {
        // Arrange
        var metodo = typeof(FotosController).GetMethod(accion)!;

        // Act
        var permiso = metodo.GetCustomAttribute<AuthorizeAttribute>();
        var anonimo = metodo.GetCustomAttribute<AllowAnonymousAttribute>();

        // Assert
        permiso.Should().NotBeNull();
        permiso!.Policy.Should().Be(PolicyNames.Trabajador);
        anonimo.Should().BeNull();
    }

    private const string Nombre = "coche.jpg";
    private Mock<IFotoService> _service = null!;
    private Mock<IStorageService> _storage = null!;
    private FotosController _controller = null!;

    [SetUp]
    public void SetUp() {
        _service = new Mock<IFotoService>();
        _storage = new Mock<IStorageService>();
        _controller = new FotosController(_service.Object, _storage.Object);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetAll_ConYSinFotos_Devuelve200ConColeccion(bool vacia) {
        // Arrange
        Foto[] fotos = vacia ? [] : [new() { Nombre = Nombre }];
        _service.Setup(s => s.GetAll()).ReturnsAsync(fotos);

        // Act
        var resultado = await _controller.GetAll();

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(fotos);
        _service.Verify(s => s.GetAll(), Times.Once);
    }

    [Test]
    public async Task GetById_Existente_Devuelve200ConMetadatos() {
        // Arrange
        var foto = new Foto { Nombre = Nombre };
        _service.Setup(s => s.GetById(Nombre)).ReturnsAsync(Result.Success<Foto, DomainError>(foto));

        // Act
        var resultado = await _controller.GetById(Nombre);

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(foto);
        _service.Verify(s => s.GetById(Nombre), Times.Once);
    }

    [Test]
    public async Task Create_AltaCorrecta_Devuelve201ConFotoYUbicacion() {
        // Arrange
        using var contenido = new MemoryStream([1, 2, 3]);
        var archivo = new FormFile(contenido, 0, contenido.Length, "archivo", Nombre);
        var foto = new Foto { Nombre = Nombre };
        _service.Setup(s => s.Create(archivo)).ReturnsAsync(Result.Success<Foto, DomainError>(foto));

        // Act
        var resultado = await _controller.Create(archivo);

        // Assert
        var respuesta = resultado.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        respuesta.StatusCode.Should().Be(201);
        respuesta.ActionName.Should().Be(nameof(FotosController.GetById));
        respuesta.RouteValues!["nombre"].Should().Be(Nombre);
        respuesta.Value.Should().BeSameAs(foto);
        _service.Verify(s => s.Create(archivo), Times.Once);
        _storage.Verify(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Delete_ResultadoCorrecto_Devuelve200SinBorrarFichero(bool borrada) {
        // Arrange
        _service.Setup(s => s.SoftDelete(Nombre)).ReturnsAsync(Result.Success<bool, DomainError>(borrada));

        // Act
        var resultado = await _controller.Delete(Nombre);

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(borrada);
        _service.Verify(s => s.SoftDelete(Nombre), Times.Once);
        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [TestCase("consulta", 400)]
    [TestCase("consulta", 404)]
    [TestCase("archivo", 400)]
    [TestCase("archivo", 404)]
    [TestCase("alta", 400)]
    [TestCase("alta", 422)]
    [TestCase("alta", 500)]
    [TestCase("baja", 400)]
    [TestCase("baja", 404)]
    public async Task Accion_ErrorDeDominio_DevuelveCodigoYCuerpoSinAccederAlDisco(string accion, int estado) {
        // Arrange
        DomainError error = estado switch {
            404 => new NotFoundError("FOTO_NO_ENCONTRADA", "Foto ausente"),
            422 => new BusinessRuleError("EXTENSION_NO_PERMITIDA", "Extension rechazada"),
            500 => new InternalError("Error guardando la foto"),
            _ => ValidationError.WithFieldErrors(new() { ["archivo"] = ["Imagen obligatoria"] })
        };
        using var contenido = new MemoryStream([1]);
        var archivo = new FormFile(contenido, 0, contenido.Length, "archivo", Nombre);
        _service.Setup(s => s.GetById(Nombre)).ReturnsAsync(Result.Failure<Foto, DomainError>(error));
        _service.Setup(s => s.Create(archivo, "vehiculos", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Foto, DomainError>(error));
        _service.Setup(s => s.SoftDelete(Nombre)).ReturnsAsync(Result.Failure<bool, DomainError>(error));

        // Act
        ActionResult respuesta = accion switch {
            "consulta" => (await _controller.GetById(Nombre)).Result!,
            "archivo" => await _controller.GetArchivo(Nombre),
            "alta" => (await _controller.Create(archivo)).Result!,
            _ => (await _controller.Delete(Nombre)).Result!
        };

        // Assert
        var objeto = respuesta.Should().BeAssignableTo<ObjectResult>().Subject;
        objeto.StatusCode.Should().Be(estado);
        if (error is ValidationError validacion)
            objeto.Value.Should().BeEquivalentTo(new { code = error.Code, message = error.Message, details = validacion.ValidationErrors });
        else objeto.Value.Should().BeEquivalentTo(new { code = error.Code, message = error.Message });
        _service.Verify(s => s.GetById(Nombre), accion is "consulta" or "archivo" ? Times.Once() : Times.Never());
        _service.Verify(s => s.Create(archivo, "vehiculos", It.IsAny<CancellationToken>()),
            accion == "alta" ? Times.Once() : Times.Never());
        _service.Verify(s => s.SoftDelete(Nombre), accion == "baja" ? Times.Once() : Times.Never());
        _storage.Verify(s => s.FileExists(It.IsAny<string>()), Times.Never);
        _storage.Verify(s => s.GetFullPath(It.IsAny<string>()), Times.Never);
        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task GetHistorico_FotoInactiva_DevuelveMetadatosSinReactivarla() {
        // Arrange
        var foto = new Foto { Nombre = Nombre, Activo = false };
        _service.Setup(s => s.GetHistoricoById(Nombre)).ReturnsAsync(Result.Success<Foto, DomainError>(foto));

        // Act
        var resultado = await _controller.GetHistorico(Nombre);

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(foto);
        foto.Activo.Should().BeFalse();
        _service.Verify(s => s.GetHistoricoById(Nombre), Times.Once);
        _service.Verify(s => s.GetById(It.IsAny<string>()), Times.Never);
        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task GetArchivo_SinFicheroFisico_Devuelve404SinAbrirArchivo() {
        // Arrange
        var foto = new Foto { Nombre = Nombre };
        var relativa = Path.Combine("vehiculos", Nombre);
        _service.Setup(s => s.GetById(Nombre)).ReturnsAsync(Result.Success<Foto, DomainError>(foto));
        _storage.Setup(s => s.FileExists(relativa)).Returns(false);

        // Act
        var resultado = await _controller.GetArchivo(Nombre);

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().BeEquivalentTo(new {
            code = "FOTO_NO_ENCONTRADA", message = $"No se ha encontrado la foto con el nombre:{Nombre}"
        });
        _service.Verify(s => s.GetById(Nombre), Times.Once);
        _storage.Verify(s => s.FileExists(relativa), Times.Once);
        _storage.Verify(s => s.GetFullPath(It.IsAny<string>()), Times.Never);
    }

    private static string CrearCarpetaTemporal() {
        var carpeta = Path.Combine(Path.GetTempPath(), "fotos-controller-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(carpeta);
        return carpeta;
    }

    [Test]
    public async Task GetArchivo_FicheroExistente_DevuelveContenidoYTipoMime() {
        // Arrange
        var carpeta = CrearCarpetaTemporal();
        var ruta = Path.Combine(carpeta, Nombre);
        await File.WriteAllBytesAsync(ruta, [1, 2, 3]);
        var relativa = Path.Combine("vehiculos", Nombre);
        _service.Setup(s => s.GetById(Nombre)).ReturnsAsync(Result.Success<Foto, DomainError>(new Foto { Nombre = Nombre }));
        _storage.Setup(s => s.FileExists(relativa)).Returns(true);
        _storage.Setup(s => s.GetFullPath(relativa)).Returns(ruta);
        try {
            // Act
            var resultado = await _controller.GetArchivo(Nombre);

            // Assert
            var respuesta = resultado.Should().BeOfType<FileStreamResult>().Subject;
            respuesta.ContentType.Should().Be("image/jpeg");
            await using var contenido = respuesta.FileStream;
            using var copia = new MemoryStream();
            await contenido.CopyToAsync(copia);
            copia.ToArray().Should().Equal(1, 2, 3);
            _service.Verify(s => s.GetById(Nombre), Times.Once);
            _storage.Verify(s => s.FileExists(relativa), Times.Once);
            _storage.Verify(s => s.GetFullPath(relativa), Times.Once);
        } finally {
            Directory.Delete(carpeta, true);
        }
    }

    [Test]
    public async Task GetArchivo_ExtensionDesconocida_UsaTipoGenerico() {
        // Arrange
        const string nombre = "coche.sinmime";
        var carpeta = CrearCarpetaTemporal();
        var ruta = Path.Combine(carpeta, nombre);
        await File.WriteAllBytesAsync(ruta, [4]);
        var relativa = Path.Combine("vehiculos", nombre);
        _service.Setup(s => s.GetById(nombre)).ReturnsAsync(Result.Success<Foto, DomainError>(new Foto { Nombre = nombre }));
        _storage.Setup(s => s.FileExists(relativa)).Returns(true);
        _storage.Setup(s => s.GetFullPath(relativa)).Returns(ruta);
        try {
            // Act
            var resultado = await _controller.GetArchivo(nombre);

            // Assert
            var respuesta = resultado.Should().BeOfType<FileStreamResult>().Subject;
            respuesta.ContentType.Should().Be("application/octet-stream");
            await respuesta.FileStream.DisposeAsync();
            _storage.Verify(s => s.FileExists(relativa), Times.Once);
            _storage.Verify(s => s.GetFullPath(relativa), Times.Once);
        } finally {
            Directory.Delete(carpeta, true);
        }
    }

    [TestCase("ausente")]
    [TestCase("sinDirectorio")]
    public async Task GetArchivo_FicheroDesaparecidoTrasComprobar_Devuelve404(string caso) {
        // Arrange
        var carpeta = CrearCarpetaTemporal();
        var relativa = Path.Combine("vehiculos", Nombre);
        var ruta = caso == "ausente"
            ? Path.Combine(carpeta, Nombre)
            : Path.Combine(carpeta, "sub", Nombre);
        _service.Setup(s => s.GetById(Nombre)).ReturnsAsync(Result.Success<Foto, DomainError>(new Foto { Nombre = Nombre }));
        _storage.Setup(s => s.FileExists(relativa)).Returns(true);
        _storage.Setup(s => s.GetFullPath(relativa)).Returns(ruta);
        try {
            // Act
            var resultado = await _controller.GetArchivo(Nombre);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().BeEquivalentTo(new {
                code = "FOTO_NO_ENCONTRADA", message = $"No se ha encontrado la foto con el nombre:{Nombre}"
            });
            _storage.Verify(s => s.FileExists(relativa), Times.Once);
            _storage.Verify(s => s.GetFullPath(relativa), Times.Once);
        } finally {
            Directory.Delete(carpeta, true);
        }
    }

    [Test]
    public async Task GetArchivoHistorico_FotoConservada_DevuelveContenido() {
        // Arrange
        var carpeta = CrearCarpetaTemporal();
        var ruta = Path.Combine(carpeta, Nombre);
        await File.WriteAllBytesAsync(ruta, [5, 6]);
        var relativa = Path.Combine("vehiculos", Nombre);
        var foto = new Foto { Nombre = Nombre, Activo = false };
        _service.Setup(s => s.GetHistoricoById(Nombre)).ReturnsAsync(Result.Success<Foto, DomainError>(foto));
        _storage.Setup(s => s.FileExists(relativa)).Returns(true);
        _storage.Setup(s => s.GetFullPath(relativa)).Returns(ruta);
        try {
            // Act
            var resultado = await _controller.GetArchivoHistorico(Nombre);

            // Assert
            var respuesta = resultado.Should().BeOfType<FileStreamResult>().Subject;
            await using var contenido = respuesta.FileStream;
            using var copia = new MemoryStream();
            await contenido.CopyToAsync(copia);
            copia.ToArray().Should().Equal(5, 6);
            _service.Verify(s => s.GetHistoricoById(Nombre), Times.Once);
            _service.Verify(s => s.GetById(It.IsAny<string>()), Times.Never);
        } finally {
            Directory.Delete(carpeta, true);
        }
    }

    [Test]
    public async Task GetArchivoHistorico_ErrorDeDominio_Devuelve404SinTocarElDisco() {
        // Arrange
        var error = new NotFoundError("FOTO_NO_ENCONTRADA", "Foto histórica ausente");
        _service.Setup(s => s.GetHistoricoById(Nombre)).ReturnsAsync(Result.Failure<Foto, DomainError>(error));

        // Act
        var resultado = await _controller.GetArchivoHistorico(Nombre);

        // Assert
        resultado.Should().BeAssignableTo<ObjectResult>().Which.StatusCode.Should().Be(404);
        _service.Verify(s => s.GetHistoricoById(Nombre), Times.Once);
        _storage.Verify(s => s.FileExists(It.IsAny<string>()), Times.Never);
        _storage.Verify(s => s.GetFullPath(It.IsAny<string>()), Times.Never);
    }
}
