using Alquiler_Vehiculos.Cache;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Marca;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Repositories.Vehiculo.Marca;
using Alquiler_Vehiculos.Services.Vehiculo.Marca;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Marca;
using FluentAssertions;
using MongoDB.Bson;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Services;

[TestFixture]
[Category("Unit")]
public class MarcaServiceTests {
    private const string Id = "64b00000000000000000000a";
    private const string Clave = "marca:" + Id;
    private Mock<IMarcaRepository> _repository = null!;
    private Mock<ICacheService> _cache = null!;
    private MarcaService _service = null!;

    [SetUp]
    public void SetUp() {
        _repository = new Mock<IMarcaRepository>();
        _cache = new Mock<ICacheService>();
        _cache.Setup(c => c.GetAsync<MarcaDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MarcaDto?)null);
        _service = new MarcaService(_repository.Object, _cache.Object,
            new CreateMarcaValidator(), new UpdateMarcaValidator());
    }

    private static MarcaModel CrearMarca() => new(ObjectId.Parse(Id), "Toyota", "Japón",
        "https://www.toyota.es", ["Corolla", "Yaris"], false, new DateTime(2026, 1, 1), null, null);

    private static CreateMarcaDto CrearDto() => new() {
        Nombre = "Toyota", Pais = "Japón", Web = "https://www.toyota.es",
        ModelosDisponibles = ["Corolla", "Yaris"]
    };

    private static UpdateMarcaDto CrearUpdate() => new() {
        Nombre = "Toyota", Pais = "Japón", Web = "https://www.toyota.es",
        ModelosDisponibles = ["Corolla", "RAV4"]
    };

    private void VerificarCacheNoEscrita() {
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<MarcaDto>(), It.IsAny<TimeSpan?>(),
            It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetAll_ConMarcasOSinEllas_DevuelveLosDtos(bool conDatos) {
        // Arrange
        _repository.Setup(r => r.GetAll()).ReturnsAsync(conDatos ? [CrearMarca()] : Array.Empty<MarcaModel>());

        // Act
        var resultado = (await _service.GetAll()).ToArray();

        // Assert
        resultado.Should().HaveCount(conDatos ? 1 : 0);
        if (conDatos) {
            resultado[0].Id.Should().Be(Id);
            resultado[0].ModelosDisponibles.Should().Equal("Corolla", "Yaris");
        }
        _repository.Verify(r => r.GetAll(), Times.Once);
        _cache.Verify(c => c.GetAsync<MarcaDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        VerificarCacheNoEscrita();
    }

    [Test]
    public async Task GetById_EnCache_NoConsultaElRepositorio() {
        // Arrange
        var dto = new MarcaDto { Id = Id, Nombre = "Toyota", ModelosDisponibles = ["Corolla"] };
        _cache.Setup(c => c.GetAsync<MarcaDto>(Clave, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        // Act
        var resultado = await _service.GetById(Id.ToUpperInvariant());

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeSameAs(dto);
        _cache.Verify(c => c.GetAsync<MarcaDto>(Clave, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);
        VerificarCacheNoEscrita();
    }

    [Test]
    public async Task GetById_SinCache_MapeaYGuardaElDto() {
        // Arrange
        _repository.Setup(r => r.GetById(Id)).ReturnsAsync(CrearMarca());

        // Act
        var resultado = await _service.GetById(Id);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().Be(Id);
        resultado.Value.ModelosDisponibles.Should().Equal("Corolla", "Yaris");
        _repository.Verify(r => r.GetById(Id), Times.Once);
        _cache.Verify(c => c.GetAsync<MarcaDto>(Clave, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.SetAsync(Clave, It.Is<MarcaDto>(d => d.Id == Id), It.IsAny<TimeSpan?>(),
            It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetById_NoExiste_Devuelve404SinGuardarCache() {
        // Arrange
        _repository.Setup(r => r.GetById(Id)).ReturnsAsync((MarcaModel?)null);

        // Act
        var resultado = await _service.GetById(Id);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(MarcaError.NotFoundMarca(Id));
        _repository.Verify(r => r.GetById(Id), Times.Once);
        _cache.Verify(c => c.GetAsync<MarcaDto>(Clave, It.IsAny<CancellationToken>()), Times.Once);
        VerificarCacheNoEscrita();
    }

    [TestCase("")]
    [TestCase("no-es-un-objectid")]
    public async Task Operaciones_IdInvalido_Devuelven404SinConsultarDependencias(string id) {
        // Arrange
        var dto = CrearUpdate();

        // Act
        var consulta = await _service.GetById(id);
        var cambio = await _service.Update(dto, id);
        var baja = await _service.SoftDelete(id);

        // Assert
        consulta.Error.Should().Be(MarcaError.NotFoundMarca(id));
        cambio.Error.Should().Be(MarcaError.NotFoundMarca(id));
        baja.Error.Should().Be(MarcaError.NotFoundMarca(id));
        _repository.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);
        _repository.Verify(r => r.Update(It.IsAny<string>(), It.IsAny<MarcaModel>()), Times.Never);
        _repository.Verify(r => r.ExistById(It.IsAny<string>()), Times.Never);
        _repository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        _cache.Verify(c => c.GetAsync<MarcaDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        VerificarCacheNoEscrita();
    }

    [Test]
    public async Task Create_DatosValidos_GuardaMarcaActivaConCopiaDeModelos() {
        // Arrange
        var dto = CrearDto();
        MarcaModel? guardada = null;
        _repository.Setup(r => r.Create(It.IsAny<MarcaModel>()))
            .ReturnsAsync((MarcaModel m) => { guardada = m; return m with { Id = ObjectId.Parse(Id) }; });

        // Act
        var resultado = await _service.Create(dto);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().Be(Id);
        resultado.Value.ModelosDisponibles.Should().Equal(dto.ModelosDisponibles);
        resultado.Value.ModelosDisponibles.Should().NotBeSameAs(guardada!.ModelosDisponibles);
        guardada.ModelosDisponibles.Should().NotBeSameAs(dto.ModelosDisponibles);
        _repository.Verify(r => r.Create(It.Is<MarcaModel>(m => m.Id == ObjectId.Empty
            && m.Nombre == dto.Nombre && m.Pais == dto.Pais && m.Web == dto.Web
            && !m.IsDeleted && m.DeletedAt == null && m.UpdatedAt == null)), Times.Once);
        _cache.Verify(c => c.RemoveAsync(Clave, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase("Nombre")]
    [TestCase("Pais")]
    [TestCase("Web")]
    [TestCase("ModelosDisponibles")]
    [TestCase("ModeloVacio")]
    [TestCase("ModelosNulos")]
    public async Task CreateYUpdate_DatosInvalidos_DevuelvenValidacionSinPersistir(string campo) {
        // Arrange
        var alta = CrearDto();
        var cambio = CrearUpdate();
        switch (campo) {
            case "Nombre": alta.Nombre = cambio.Nombre = ""; break;
            case "Pais": alta.Pais = cambio.Pais = ""; break;
            case "Web": alta.Web = cambio.Web = ""; break;
            case "ModelosDisponibles": alta.ModelosDisponibles = []; cambio.ModelosDisponibles = []; break;
            case "ModeloVacio": alta.ModelosDisponibles = [" "]; cambio.ModelosDisponibles = [" "]; break;
            case "ModelosNulos": alta.ModelosDisponibles = null!; cambio.ModelosDisponibles = null!; break;
        }
        var propiedad = campo == "ModeloVacio" ? "ModelosDisponibles[0]"
            : campo == "ModelosNulos" ? "ModelosDisponibles" : campo;

        // Act
        var creacion = await _service.Create(alta);
        var actualizacion = await _service.Update(cambio, Id);

        // Assert
        creacion.IsFailure.Should().BeTrue();
        actualizacion.IsFailure.Should().BeTrue();
        creacion.Error.Should().BeOfType<ValidationError>().Which.ValidationErrors.Should().ContainKey(propiedad);
        actualizacion.Error.Should().BeOfType<ValidationError>().Which.ValidationErrors.Should().ContainKey(propiedad);
        _repository.Verify(r => r.Create(It.IsAny<MarcaModel>()), Times.Never);
        _repository.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);
        _repository.Verify(r => r.Update(It.IsAny<string>(), It.IsAny<MarcaModel>()), Times.Never);
        VerificarCacheNoEscrita();
    }

    [Test]
    public async Task Update_DatosValidos_SustituyeModelosYConservaIdYAuditoria() {
        // Arrange
        var original = CrearMarca();
        var dto = CrearUpdate();
        dto.Nombre = "Toyota Motor";
        dto.Pais = "Japón actualizado";
        dto.Web = "https://toyota.com";
        MarcaModel? enviada = null;
        _repository.Setup(r => r.GetById(Id)).ReturnsAsync(original);
        _repository.Setup(r => r.Update(Id, It.IsAny<MarcaModel>()))
            .ReturnsAsync((string _, MarcaModel m) => { enviada = m; return m; });

        // Act
        var resultado = await _service.Update(dto, Id.ToUpperInvariant());

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.ModelosDisponibles.Should().Equal("Corolla", "RAV4");
        resultado.Value.ModelosDisponibles.Should().NotContain("Yaris");
        original.ModelosDisponibles.Should().Equal("Corolla", "Yaris");
        enviada!.ModelosDisponibles.Should().NotBeSameAs(dto.ModelosDisponibles);
        resultado.Value.ModelosDisponibles.Should().NotBeSameAs(enviada.ModelosDisponibles);
        _repository.Verify(r => r.GetById(Id), Times.Once);
        _repository.Verify(r => r.Update(Id, It.Is<MarcaModel>(m => m.Id == original.Id
            && m.CreatedAt == original.CreatedAt && m.UpdatedAt == original.UpdatedAt
            && m.DeletedAt == original.DeletedAt && m.IsDeleted == original.IsDeleted
            && m.Nombre == dto.Nombre && m.Pais == dto.Pais && m.Web == dto.Web)), Times.Once);
        _cache.Verify(c => c.RemoveAsync(Clave, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Update_MarcaAusenteAntesODuranteElCambio_Devuelve404(bool desapareceAlGuardar) {
        // Arrange
        _repository.Setup(r => r.GetById(Id)).ReturnsAsync(desapareceAlGuardar ? CrearMarca() : null);
        _repository.Setup(r => r.Update(Id, It.IsAny<MarcaModel>())).ReturnsAsync((MarcaModel?)null);

        // Act
        var resultado = await _service.Update(CrearUpdate(), Id);

        // Assert
        resultado.Error.Should().Be(MarcaError.NotFoundMarca(Id));
        _repository.Verify(r => r.GetById(Id), Times.Once);
        _repository.Verify(r => r.Update(Id, It.IsAny<MarcaModel>()), desapareceAlGuardar ? Times.Once() : Times.Never());
        VerificarCacheNoEscrita();
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task SoftDelete_SegunResultado_DevuelveBajaOErrorSinBorrarFisicamente(bool existe, bool borrada) {
        // Arrange
        _repository.Setup(r => r.ExistById(Id)).ReturnsAsync(existe);
        _repository.Setup(r => r.SoftDelete(Id)).ReturnsAsync(borrada);

        // Act
        var resultado = await _service.SoftDelete(Id);

        // Assert
        resultado.IsSuccess.Should().Be(existe && borrada);
        if (!existe) resultado.Error.Should().Be(MarcaError.NotFoundMarca(Id));
        else if (!borrada) resultado.Error.Should().Be(MarcaError.BorradoError());
        else resultado.Value.Should().BeTrue();
        _repository.Verify(r => r.ExistById(Id), Times.Once);
        _repository.Verify(r => r.SoftDelete(Id), existe ? Times.Once() : Times.Never());
        _repository.Verify(r => r.Update(It.IsAny<string>(), It.IsAny<MarcaModel>()), Times.Never);
        _cache.Verify(c => c.RemoveAsync(Clave, It.IsAny<CancellationToken>()), existe && borrada ? Times.Once() : Times.Never());
    }
}
