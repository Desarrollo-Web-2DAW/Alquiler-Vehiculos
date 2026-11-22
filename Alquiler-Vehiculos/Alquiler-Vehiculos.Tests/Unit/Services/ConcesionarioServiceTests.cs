using Alquiler_Vehiculos.Cache;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Concesionario;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Repositories.Vehiculo.Concesionario;
using Alquiler_Vehiculos.Services.Vehiculo.Concesonario;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Concesionario;
using FluentAssertions;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Services;

/// <summary>
/// Tests unitarios de <see cref="ConcesionarioService"/>.
/// Se mockean el repositorio y la caché (las dos fronteras de E/S del servicio) y se
/// usan los validadores reales, porque son deterministas y forman parte del contrato
/// que el servicio debe respetar.
/// </summary>
[TestFixture]
[Category("Unit")]
public class ConcesionarioServiceTests
{
    private const long IdExistente = 5;
    private const string TelefonoValido = "942000111";
    private const string EmailValido = "contacto@motornorte.es";
    private const string ClaveConcesionario = "concesionario:5";

    private Mock<IConcesionarioRepository> _repository = null!;
    private Mock<ICacheService> _cache = null!;
    private ConcesionarioService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IConcesionarioRepository>();
        _cache = new Mock<ICacheService>();

        // Por defecto el concesionario no tiene vehículos asociados: cada test que
        // necesite vehículos lo sobreescribe con su propio Setup.
        _repository.Setup(r => r.GetVehiculos(It.IsAny<long>())).ReturnsAsync([]);

        _service = new ConcesionarioService(
            _repository.Object,
            _cache.Object,
            new CreateConcesionarioValidator(),
            new UpdateConcesionarioValidator());
    }

    private static CreateConcesionarioDto CrearCreateValido() => new()
    {
        Nombre = "Motor Norte",
        Ciudad = "Santander",
        Telefono = TelefonoValido,
        Email = EmailValido
    };

    private static UpdateConcesionarioDto CrearUpdateValido(string telefono = TelefonoValido) => new()
    {
        Nombre = "Motor Norte",
        Ciudad = "Santander",
        Telefono = telefono,
        Email = EmailValido
    };

    private static ConcesionarioModel CrearModelo(string telefono = TelefonoValido, long id = IdExistente) =>
        new(id, "Motor Norte", "Santander", telefono, EmailValido, false,
            new DateTime(2025, 1, 1), null, null);

    private static ConcesionarioDto CrearDtoEsperado(string telefono = TelefonoValido) => new()
    {
        Nombre = "Motor Norte",
        Ciudad = "Santander",
        Telefono = telefono,
        Email = EmailValido
    };

    private static VehiculoModel CrearVehiculo(string id, string matricula) =>
        new() { Id = id, Matrícula = matricula };

    [Test]
    public async Task GetAll_ConConcesionarios_DevuelveTodosMapeados()
    {
        // Arrange
        _repository.Setup(r => r.GetAll())
            .ReturnsAsync([CrearModelo(TelefonoValido, 1), CrearModelo("942000222", 2)]);

        // Act
        var resultado = await _service.GetAll();

        // Assert
        resultado.Should().BeEquivalentTo([CrearDtoEsperado(), CrearDtoEsperado("942000222")]);

        _repository.Verify(r => r.GetAll(), Times.Once);
    }

    [Test]
    public async Task GetAll_SinConcesionarios_DevuelveColeccionVacia()
    {
        // Arrange
        _repository.Setup(r => r.GetAll()).ReturnsAsync([]);

        // Act
        var resultado = await _service.GetAll();

        // Assert
        resultado.Should().BeEmpty();

        _repository.Verify(r => r.GetAll(), Times.Once);
    }

    [Test]
    public async Task GetById_CuandoEstaEnCache_DevuelveElDtoSinConsultarElRepositorio()
    {
        // Arrange
        _cache.Setup(c => c.GetAsync<ConcesionarioDto>(ClaveConcesionario, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CrearDtoEsperado());

        // Act
        var resultado = await _service.GetById(IdExistente);

        // Assert
        resultado.Value.Should().BeEquivalentTo(CrearDtoEsperado());

        _repository.Verify(r => r.GetById(It.IsAny<long>()), Times.Never);
        _cache.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<ConcesionarioDto>(), It.IsAny<TimeSpan?>(),
                It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task GetById_CuandoNoEstaEnCacheYExiste_LoGuardaEnCacheYDevuelveElDto()
    {
        // Arrange
        _cache.Setup(c => c.GetAsync<ConcesionarioDto>(ClaveConcesionario, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConcesionarioDto?)null);
        _repository.Setup(r => r.GetById(IdExistente)).ReturnsAsync(CrearModelo());

        // Act
        var resultado = await _service.GetById(IdExistente);

        // Assert
        resultado.Value.Should().BeEquivalentTo(CrearDtoEsperado());

        _cache.Verify(
            c => c.SetAsync(ClaveConcesionario,
                It.Is<ConcesionarioDto>(d => ReferenceEquals(d, resultado.Value)),
                It.IsAny<TimeSpan?>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetById_CuandoNoExiste_DevuelveNotFound()
    {
        // Arrange
        _cache.Setup(c => c.GetAsync<ConcesionarioDto>(ClaveConcesionario, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConcesionarioDto?)null);
        _repository.Setup(r => r.GetById(IdExistente)).ReturnsAsync((ConcesionarioModel?)null);

        // Act
        var resultado = await _service.GetById(IdExistente);

        // Assert
        resultado.Error.Should().Be(ConcesionarioError.NotFoundConcesionario(IdExistente));

        _cache.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<ConcesionarioDto>(), It.IsAny<TimeSpan?>(),
                It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Create_ConDatosInvalidos_DevuelveValidationErrorConLosCampos()
    {
        // Arrange
        var dto = CrearCreateValido();
        dto.Telefono = "123";

        // Act
        var resultado = await _service.Create(dto);

        // Assert
        var error = resultado.Error.Should().BeOfType<ValidationError>().Subject;
        error.ValidationErrors.Should().ContainKey("Telefono")
            .WhoseValue.Should().Contain("El teléfono debe tener 9 cifras y empezar por 6, 7 o 9");

        _repository.Verify(r => r.ExistsByTelefono(It.IsAny<string>()), Times.Never);
        _repository.Verify(r => r.Create(It.IsAny<ConcesionarioModel>()), Times.Never);
    }

    [Test]
    public async Task Create_ConTelefonoDuplicado_DevuelveConflict()
    {
        // Arrange
        var dto = CrearCreateValido();
        _repository.Setup(r => r.ExistsByTelefono(TelefonoValido)).ReturnsAsync(true);

        // Act
        var resultado = await _service.Create(dto);

        // Assert
        resultado.Error.Should().Be(ConcesionarioError.ConcesionarioYaExiste(TelefonoValido));

        _repository.Verify(r => r.Create(It.IsAny<ConcesionarioModel>()), Times.Never);
    }

    [Test]
    public async Task Create_ConDatosValidos_CreaYDevuelveElDto()
    {
        // Arrange
        var dto = CrearCreateValido();
        _repository.Setup(r => r.ExistsByTelefono(TelefonoValido)).ReturnsAsync(false);
        _repository.Setup(r => r.Create(It.IsAny<ConcesionarioModel>())).ReturnsAsync(CrearModelo());

        // Act
        var resultado = await _service.Create(dto);

        // Assert
        resultado.Value.Should().BeEquivalentTo(CrearDtoEsperado());

        _repository.Verify(r => r.Create(It.Is<ConcesionarioModel>(m =>
            m.Id == 0 && m.Nombre == "Motor Norte" && m.Ciudad == "Santander" &&
            m.Telefono == TelefonoValido && m.Email == EmailValido && !m.IsDeleted)), Times.Once);
    }

    [Test]
    public async Task Update_ConDatosInvalidos_DevuelveValidationErrorSinConsultarElRepositorio()
    {
        // Arrange
        var dto = CrearUpdateValido();
        dto.Email = "no-es-un-email";

        // Act
        var resultado = await _service.Update(dto, IdExistente);

        // Assert
        var error = resultado.Error.Should().BeOfType<ValidationError>().Subject;
        error.ValidationErrors.Should().ContainKey("Email");

        _repository.Verify(r => r.GetById(It.IsAny<long>()), Times.Never);
        _repository.Verify(r => r.Update(It.IsAny<long>(), It.IsAny<ConcesionarioModel>()), Times.Never);
    }

    [Test]
    public async Task Update_CuandoNoExiste_DevuelveNotFound()
    {
        // Arrange
        _repository.Setup(r => r.GetById(IdExistente)).ReturnsAsync((ConcesionarioModel?)null);

        // Act
        var resultado = await _service.Update(CrearUpdateValido(), IdExistente);

        // Assert
        resultado.Error.Should().Be(ConcesionarioError.NotFoundConcesionario(IdExistente));

        _repository.Verify(r => r.Update(It.IsAny<long>(), It.IsAny<ConcesionarioModel>()), Times.Never);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Update_CuandoElTelefonoCambiaYEstaEnUso_DevuelveConflict()
    {
        // Arrange
        const string telefonoNuevo = "942000222";
        _repository.Setup(r => r.GetById(IdExistente)).ReturnsAsync(CrearModelo());
        _repository.Setup(r => r.ExistsByTelefono(telefonoNuevo)).ReturnsAsync(true);

        // Act
        var resultado = await _service.Update(CrearUpdateValido(telefonoNuevo), IdExistente);

        // Assert
        resultado.Error.Should().Be(ConcesionarioError.ConcesionarioYaExiste(telefonoNuevo));

        _repository.Verify(r => r.Update(It.IsAny<long>(), It.IsAny<ConcesionarioModel>()), Times.Never);
    }

    [Test]
    public async Task Update_CuandoElTelefonoNoCambia_NoCompruebaDuplicadosYActualiza()
    {
        // Arrange
        _repository.Setup(r => r.GetById(IdExistente)).ReturnsAsync(CrearModelo());
        _repository.Setup(r => r.Update(IdExistente, It.IsAny<ConcesionarioModel>()))
            .ReturnsAsync(CrearModelo());

        // Act
        var resultado = await _service.Update(CrearUpdateValido(TelefonoValido), IdExistente);

        // Assert
        resultado.Value.Should().BeEquivalentTo(CrearDtoEsperado());

        _repository.Verify(r => r.ExistsByTelefono(It.IsAny<string>()), Times.Never);
        _repository.Verify(r => r.Update(IdExistente, It.IsAny<ConcesionarioModel>()), Times.Once);
    }

    [Test]
    public async Task Update_CuandoElTelefonoCambiaYEstaLibre_ActualizaCorrectamente()
    {
        // Arrange
        const string telefonoNuevo = "942000222";
        _repository.Setup(r => r.GetById(IdExistente)).ReturnsAsync(CrearModelo());
        _repository.Setup(r => r.ExistsByTelefono(telefonoNuevo)).ReturnsAsync(false);
        _repository.Setup(r => r.Update(IdExistente, It.IsAny<ConcesionarioModel>()))
            .ReturnsAsync(CrearModelo(telefonoNuevo));

        // Act
        var resultado = await _service.Update(CrearUpdateValido(telefonoNuevo), IdExistente);

        // Assert
        resultado.Value.Should().BeEquivalentTo(CrearDtoEsperado(telefonoNuevo));

        _repository.Verify(r => r.Update(IdExistente, It.Is<ConcesionarioModel>(m =>
            m.Telefono == telefonoNuevo)), Times.Once);
    }

    [Test]
    public async Task Update_CuandoActualiza_InvalidaLaCacheDelConcesionarioYDeSusVehiculos()
    {
        // Arrange
        _repository.Setup(r => r.GetById(IdExistente)).ReturnsAsync(CrearModelo());
        _repository.Setup(r => r.Update(IdExistente, It.IsAny<ConcesionarioModel>()))
            .ReturnsAsync(CrearModelo());
        _repository.Setup(r => r.GetVehiculos(IdExistente))
            .ReturnsAsync([CrearVehiculo("abc123", "1111AAA"), CrearVehiculo("def456", "2222BBB")]);

        // Act
        var resultado = await _service.Update(CrearUpdateValido(), IdExistente);

        // Assert
        resultado.IsSuccess.Should().BeTrue();

        _cache.Verify(c => c.RemoveAsync(ClaveConcesionario, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync("vehiculo:id:abc123", It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync("vehiculo:matricula:1111AAA", It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync("vehiculo:id:def456", It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync("vehiculo:matricula:2222BBB", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Update_CuandoElRepositorioNoDevuelveLaEntidad_DevuelveNotFoundYNoInvalidaCache()
    {
        // Arrange
        _repository.Setup(r => r.GetById(IdExistente)).ReturnsAsync(CrearModelo());
        _repository.Setup(r => r.Update(IdExistente, It.IsAny<ConcesionarioModel>()))
            .ReturnsAsync((ConcesionarioModel?)null);

        // Act
        var resultado = await _service.Update(CrearUpdateValido(), IdExistente);

        // Assert
        resultado.Error.Should().Be(ConcesionarioError.NotFoundConcesionario(IdExistente));

        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Update_CuandoActualiza_ConservaElIdYLaFechaDeCreacionDelRegistroExistente()
    {
        // Arrange
        var existente = CrearModelo();
        _repository.Setup(r => r.GetById(IdExistente)).ReturnsAsync(existente);
        _repository.Setup(r => r.Update(IdExistente, It.IsAny<ConcesionarioModel>()))
            .ReturnsAsync(existente);

        // Act
        await _service.Update(CrearUpdateValido(), IdExistente);

        // Assert
        _repository.Verify(r => r.Update(IdExistente, It.Is<ConcesionarioModel>(m =>
            m.Id == existente.Id && m.CreatedAt == existente.CreatedAt)), Times.Once);
    }

    [Test]
    public async Task SoftDelete_CuandoTieneVehiculos_DevuelveBusinessRuleError()
    {
        // Arrange
        _repository.Setup(r => r.ExistById(IdExistente)).ReturnsAsync(true);
        _repository.Setup(r => r.HasVehiculos(IdExistente)).ReturnsAsync(true);

        // Act
        var resultado = await _service.SoftDelete(IdExistente);

        // Assert
        resultado.Error.Should().Be(
            new BusinessRuleError("CONCESIONARIO_CON_VEHICULOS", "No se puede borrar un concesionario con vehiculos asociados"));

        _repository.Verify(r => r.SoftDelete(It.IsAny<long>()), Times.Never);
    }

    [Test]
    public async Task SoftDelete_CuandoNoExiste_DevuelveNotFound()
    {
        // Arrange
        _repository.Setup(r => r.ExistById(IdExistente)).ReturnsAsync(false);

        // Act
        var resultado = await _service.SoftDelete(IdExistente);

        // Assert
        resultado.Error.Should().Be(ConcesionarioError.NotFoundConcesionario(IdExistente));

        _repository.Verify(r => r.HasVehiculos(It.IsAny<long>()), Times.Never);
        _repository.Verify(r => r.SoftDelete(It.IsAny<long>()), Times.Never);
    }

    [Test]
    public async Task SoftDelete_CuandoElRepositorioFalla_DevuelveBorradoError()
    {
        // Arrange
        _repository.Setup(r => r.HasVehiculos(IdExistente)).ReturnsAsync(false);
        _repository.Setup(r => r.ExistById(IdExistente)).ReturnsAsync(true);
        _repository.Setup(r => r.SoftDelete(IdExistente)).ReturnsAsync(false);

        // Act
        var resultado = await _service.SoftDelete(IdExistente);

        // Assert
        resultado.Error.Should().Be(ConcesionarioError.BorradoError());

        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SoftDelete_CuandoSeBorra_DevuelveTrueEInvalidaLaCache()
    {
        // Arrange
        _repository.Setup(r => r.HasVehiculos(IdExistente)).ReturnsAsync(false);
        _repository.Setup(r => r.ExistById(IdExistente)).ReturnsAsync(true);
        _repository.Setup(r => r.SoftDelete(IdExistente)).ReturnsAsync(true);

        // Act
        var resultado = await _service.SoftDelete(IdExistente);

        // Assert
        resultado.Value.Should().BeTrue();

        _cache.Verify(c => c.RemoveAsync(ClaveConcesionario, It.IsAny<CancellationToken>()), Times.Once);
    }
}
