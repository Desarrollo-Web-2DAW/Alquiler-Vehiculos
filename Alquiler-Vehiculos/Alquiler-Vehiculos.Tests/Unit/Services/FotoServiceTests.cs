using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Storage;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Services.Storage;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Services;

[TestFixture]
[Category("Unit")]
public class FotoServiceTests {
    private const string Nombre = "coche.jpg";
    private Mock<IFotoRepository> _repository = null!;
    private Mock<IStorageService> _storage = null!;
    private FotoService _service = null!;

    [SetUp]
    public void SetUp() {
        _repository = new Mock<IFotoRepository>();
        _storage = new Mock<IStorageService>();
        _service = new FotoService(_repository.Object, _storage.Object);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Create_ArchivoNuloOVacio_DevuelveValidacionSinGuardar(bool nulo) {
        // Arrange
        using var contenido = new MemoryStream();
        IFormFile? archivo = nulo ? null : new FormFile(contenido, 0, 0, "archivo", Nombre);
        _storage.Setup(s => s.IsEmpty(archivo)).Returns(true);

        // Act
        var resultado = await _service.Create(archivo);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        var error = resultado.Error.Should().BeOfType<ValidationError>().Subject;
        error.ValidationErrors.Should().ContainKey("archivo").WhoseValue.Should().Equal("Debe proporcionar una imagen con contenido.");
        _storage.Verify(s => s.IsEmpty(archivo), nulo ? Times.Never() : Times.Once());
        _storage.Verify(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
        _repository.Verify(r => r.Create(It.IsAny<Foto>()), Times.Never);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Create_ArchivoConContenido_PropagaFotoOErrorDeStorage(bool correcto) {
        // Arrange
        using var contenido = new MemoryStream([1, 2, 3]);
        var archivo = new FormFile(contenido, 0, contenido.Length, "archivo", Nombre);
        var foto = new Foto { Nombre = Nombre };
        var error = StorageError.ExtensionNoPermitida();
        _storage.Setup(s => s.IsEmpty(archivo)).Returns(false);
        _storage.Setup(s => s.SaveFileAsync(archivo, "vehiculos")).ReturnsAsync(correcto
            ? Result.Success<string, DomainError>(Nombre) : Result.Failure<string, DomainError>(error));
        _repository.Setup(r => r.GetById(Nombre)).ReturnsAsync(foto);

        // Act
        var resultado = await _service.Create(archivo);

        // Assert
        resultado.IsSuccess.Should().Be(correcto);
        if (correcto) resultado.Value.Should().BeSameAs(foto);
        else resultado.Error.Should().Be(error);
        _storage.Verify(s => s.IsEmpty(archivo), Times.Once);
        _storage.Verify(s => s.SaveFileAsync(archivo, "vehiculos"), Times.Once);
        _repository.Verify(r => r.GetById(Nombre), correcto ? Times.Once() : Times.Never());
        _repository.Verify(r => r.Create(It.IsAny<Foto>()), Times.Never);
    }

    [Test]
    public async Task Create_ArchivoGuardadoSinMetadatos_DevuelveFotoNoEncontrada() {
        // Arrange
        using var contenido = new MemoryStream([1, 2, 3]);
        var archivo = new FormFile(contenido, 0, contenido.Length, "archivo", Nombre);
        _storage.Setup(s => s.IsEmpty(archivo)).Returns(false);
        _storage.Setup(s => s.SaveFileAsync(archivo, "vehiculos"))
            .ReturnsAsync(Result.Success<string, DomainError>(Nombre));
        _repository.Setup(r => r.GetById(Nombre)).ReturnsAsync((Foto?)null);

        // Act
        var resultado = await _service.Create(archivo);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(FotoError.FotoNotFound(Nombre));
        _storage.Verify(s => s.IsEmpty(archivo), Times.Once);
        _storage.Verify(s => s.SaveFileAsync(archivo, "vehiculos"), Times.Once);
        _repository.Verify(r => r.GetById(Nombre), Times.Once);
        _repository.Verify(r => r.Create(It.IsAny<Foto>()), Times.Never);
    }

    [Test]
    public async Task GetAll_FotosConDistintosEstados_DevuelveSoloActivasSinFechaDeBaja() {
        // Arrange
        var activa = new Foto { Nombre = Nombre, Activo = true };
        _repository.Setup(r => r.GetAll()).ReturnsAsync([
            activa, new() { Nombre = "inactiva.jpg", Activo = false },
            new() { Nombre = "borrada.jpg", Activo = true, DeletedAt = new(2026, 1, 1) }
        ]);

        // Act
        var resultado = await _service.GetAll();

        // Assert
        resultado.Should().ContainSingle().Which.Should().BeSameAs(activa);
        _repository.Verify(r => r.GetAll(), Times.Once);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("..")]
    [TestCase("otra/foto.jpg")]
    [TestCase("otra\\foto.jpg")]
    public async Task GetById_NombreNoValido_NoConsultaRepositorio(string? nombre) {
        // Arrange
        var entrada = nombre!;

        // Act
        var resultado = await _service.GetById(entrada);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>().Which.Message.Should().Be("El identificador debe ser un nombre de foto, no una ruta.");
        _repository.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task GetById_RutaEnraizadaSinSeparadores_RechazaSiElSistemaLaReconoce() {
        // Arrange
        // En Windows C:foto.jpg está enraizada sin contener barras. En Unix los nombres con ':' son válidos.
        const string nombre = "C:foto.jpg";
        var foto = new Foto { Nombre = nombre, Activo = true };
        _repository.Setup(r => r.GetById(nombre)).ReturnsAsync(foto);

        // Act
        var resultado = await _service.GetById(nombre);

        // Assert
        resultado.IsFailure.Should().Be(Path.IsPathRooted(nombre));
        if (Path.IsPathRooted(nombre)) resultado.Error.Should().BeOfType<ValidationError>();
        else resultado.Value.Should().BeSameAs(foto);
        _repository.Verify(r => r.GetById(nombre), Path.IsPathRooted(nombre) ? Times.Never() : Times.Once());
    }

    [TestCase("ausente")]
    [TestCase("inactiva")]
    [TestCase("borrada")]
    [TestCase("activa")]
    public async Task GetById_NombreValido_RespetaEstadoDelCatalogo(string estado) {
        // Arrange
        var foto = estado == "ausente" ? null : new Foto {
            Nombre = Nombre, Activo = estado != "inactiva", DeletedAt = estado == "borrada" ? new DateTime(2026, 1, 1) : null
        };
        _repository.Setup(r => r.GetById(Nombre)).ReturnsAsync(foto);

        // Act
        var resultado = await _service.GetById(Nombre);

        // Assert
        resultado.IsSuccess.Should().Be(estado == "activa");
        if (estado == "activa") resultado.Value.Should().BeSameAs(foto);
        else resultado.Error.Should().Be(FotoError.FotoNotFound(Nombre));
        _repository.Verify(r => r.GetById(Nombre), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task SoftDelete_FotoExistente_PropagaResultadoSinEliminarFichero(bool borrada) {
        // Arrange
        _repository.Setup(r => r.GetById(Nombre)).ReturnsAsync(new Foto { Nombre = Nombre, Activo = true });
        _repository.Setup(r => r.SoftDelete(Nombre)).ReturnsAsync(borrada);

        // Act
        var resultado = await _service.SoftDelete(Nombre);

        // Assert
        resultado.IsSuccess.Should().Be(borrada);
        if (borrada) resultado.Value.Should().BeTrue();
        else resultado.Error.Should().Be(FotoError.FotoNotFound(Nombre));
        _repository.Verify(r => r.GetById(Nombre), Times.Once);
        _repository.Verify(r => r.SoftDelete(Nombre), Times.Once);
        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [TestCase("..", false)]
    [TestCase(Nombre, true)]
    public async Task SoftDelete_NombreInvalidoOAusente_NoBorraNiEliminaFichero(string nombre, bool consulta) {
        // Arrange
        _repository.Setup(r => r.GetById(nombre)).ReturnsAsync((Foto?)null);

        // Act
        var resultado = await _service.SoftDelete(nombre);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        if (consulta) resultado.Error.Should().Be(FotoError.FotoNotFound(nombre));
        else resultado.Error.Should().BeOfType<ValidationError>();
        _repository.Verify(r => r.GetById(nombre), consulta ? Times.Once() : Times.Never());
        _repository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }
}
