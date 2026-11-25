using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Storage;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Services.Storage;
using Alquiler_Vehiculos.Validators.Storage;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Services;

[TestFixture]
[Category("Unit")]
public class FotoServiceTests {
    private const string Nombre = "foto-generada.jpg";
    private Mock<IFotoRepository> _repository = null!;
    private Mock<IStorageService> _storage = null!;
    private FotoService _service = null!;

    [SetUp]
    public void SetUp() {
        _repository = new Mock<IFotoRepository>();
        _storage = new Mock<IStorageService>();
        var settings = Options.Create(new StorageSettings {
            MaxFileSize = 1024,
            AllowedExtensions = [".jpg"],
            AllowedContentTypes = ["image/jpeg"]
        });
        _service = new FotoService(new FileValidator(settings), _repository.Object, _storage.Object);
    }

    private static IFormFile CrearArchivo(string nombre = "coche.jpg", string tipo = "image/jpeg") {
        var contenido = new MemoryStream([1, 2, 3]);
        return new FormFile(contenido, 0, contenido.Length, "archivo", nombre) {
            Headers = new HeaderDictionary(), ContentType = tipo
        };
    }

    private void PrepararStorage(Result<Foto, DomainError>? resultado = null) =>
        _storage.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), "vehiculos", It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultado ?? Result.Success<Foto, DomainError>(new Foto {
                Nombre = Nombre, Ruta = $"/uploads/vehiculos/{Nombre}", Extension = ".jpg", Tamano = 3
            }));

    [Test]
    public async Task PrepareAsync_ArchivoInvalido_DevuelveValidacionSinEscribirNiPersistir() {
        var resultado = await _service.PrepareAsync(CrearArchivo("coche.pdf", "application/pdf"));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>();
        _storage.Verify(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.Create(It.IsAny<Foto>()), Times.Never);
    }

    [Test]
    public async Task PrepareAsync_ArchivoValido_EscribeSinCrearRegistro() {
        PrepararStorage();
        var archivo = CrearArchivo();

        var resultado = await _service.PrepareAsync(archivo);

        resultado.IsSuccess.Should().BeTrue();
        _storage.Verify(s => s.SaveFileAsync(archivo, "vehiculos",
            It.Is<string>(name => name.EndsWith(".jpg")), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.Create(It.IsAny<Foto>()), Times.Never);
    }

    [Test]
    public async Task Create_ArchivoValido_PersisteLaFotoPreparada() {
        PrepararStorage();
        var fotoPersistida = new Foto { Nombre = Nombre, Ruta = $"/uploads/vehiculos/{Nombre}" };
        _repository.Setup(r => r.Create(It.IsAny<Foto>())).ReturnsAsync(fotoPersistida);

        var resultado = await _service.Create(CrearArchivo());

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeSameAs(fotoPersistida);
        _repository.Verify(r => r.Create(It.Is<Foto>(f => f.Nombre == Nombre)), Times.Once);
    }

    [Test]
    public async Task Create_FallaLaPersistencia_EliminaElArchivoPreparado() {
        PrepararStorage();
        _repository.Setup(r => r.Create(It.IsAny<Foto>()))
            .ThrowsAsync(new InvalidOperationException("Fallo de persistencia"));
        _storage.Setup(s => s.DeleteFileAsync(It.IsAny<string>()))
            .ReturnsAsync(Result.Success<bool, DomainError>(true));

        var resultado = await _service.Create(CrearArchivo());

        resultado.IsFailure.Should().BeTrue();
        _storage.Verify(s => s.DeleteFileAsync(It.Is<string>(path =>
            path.StartsWith($"vehiculos{Path.DirectorySeparatorChar}", StringComparison.Ordinal))), Times.Once);
    }

    [Test]
    public async Task GetAll_FotosConDistintosEstados_DevuelveSoloActivas() {
        var activa = new Foto { Nombre = Nombre, Activo = true };
        _repository.Setup(r => r.GetAll()).ReturnsAsync([
            activa, new() { Nombre = "inactiva.jpg", Activo = false },
            new() { Nombre = "borrada.jpg", Activo = true, DeletedAt = new(2026, 1, 1) }
        ]);

        var resultado = await _service.GetAll();

        resultado.Should().ContainSingle().Which.Should().BeSameAs(activa);
    }

    [Test]
    public async Task GetById_FotoRetirada_DevuelveLaFotoHistorica() {
        var foto = new Foto { Nombre = Nombre, Activo = false, DeletedAt = new(2026, 1, 1) };
        _repository.Setup(r => r.GetById(Nombre)).ReturnsAsync(foto);

        var resultado = await _service.GetById(Nombre);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeSameAs(foto);
    }

    [Test]
    public async Task GetHistoricoById_UsaLaConsultaHistorica() {
        var foto = new Foto { Nombre = Nombre, Activo = false };
        _repository.Setup(r => r.GetById(Nombre)).ReturnsAsync(foto);

        var resultado = await _service.GetHistoricoById(Nombre);

        resultado.Value.Should().BeSameAs(foto);
        _repository.Verify(r => r.GetById(Nombre), Times.Once);
    }

    [Test]
    public async Task SoftDelete_FotoReferenciada_DevuelveConflictSinDarDeBaja() {
        _repository.Setup(r => r.GetById(Nombre)).ReturnsAsync(new Foto { Nombre = Nombre, Activo = true });
        _repository.Setup(r => r.IsReferencedByVehicle(Nombre, null)).ReturnsAsync(true);

        var resultado = await _service.SoftDelete(Nombre);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(FotoError.FotoReferenciada(Nombre));
        _repository.Verify(r => r.SoftDeleteSiNoAsociada(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Test]
    public async Task SoftDelete_FotoNoReferenciada_RealizaBajaLogicaSinBorrarBytes() {
        _repository.Setup(r => r.GetById(Nombre)).ReturnsAsync(new Foto { Nombre = Nombre, Activo = true });
        _repository.Setup(r => r.IsReferencedByVehicle(Nombre, null)).ReturnsAsync(false);
        _repository.Setup(r => r.SoftDeleteSiNoAsociada(Nombre, null)).ReturnsAsync(true);

        var resultado = await _service.SoftDelete(Nombre);

        resultado.IsSuccess.Should().BeTrue();
        _repository.Verify(r => r.SoftDeleteSiNoAsociada(Nombre, null), Times.Once);
        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task HardDelete_FotoReferenciada_BloqueaBorradoFisico() {
        _repository.Setup(r => r.GetById(Nombre)).ReturnsAsync(new Foto { Nombre = Nombre });
        _repository.Setup(r => r.IsReferencedByVehicle(Nombre, null)).ReturnsAsync(true);

        var resultado = await _service.HardDelete(Nombre);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(FotoError.FotoReferenciada(Nombre));
        _repository.Verify(r => r.HardDelete(It.IsAny<string>()), Times.Never);
        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task HardDelete_FotoSinReferencias_EliminaFilaYArchivo() {
        _repository.Setup(r => r.GetById(Nombre)).ReturnsAsync(new Foto { Nombre = Nombre });
        _repository.Setup(r => r.IsReferencedByVehicle(Nombre, null)).ReturnsAsync(false);
        _repository.Setup(r => r.HardDelete(Nombre)).ReturnsAsync(true);
        _storage.Setup(s => s.DeleteFileAsync(Path.Combine("vehiculos", Nombre)))
            .ReturnsAsync(Result.Success<bool, DomainError>(true));

        var resultado = await _service.HardDelete(Nombre);

        resultado.IsSuccess.Should().BeTrue();
        _repository.Verify(r => r.HardDelete(Nombre), Times.Once);
        _storage.Verify(s => s.DeleteFileAsync(Path.Combine("vehiculos", Nombre)), Times.Once);
    }
}
