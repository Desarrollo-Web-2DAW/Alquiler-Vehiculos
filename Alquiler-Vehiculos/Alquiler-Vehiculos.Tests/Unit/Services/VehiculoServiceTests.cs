using Alquiler_Vehiculos.Cache;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Storage;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;
using Alquiler_Vehiculos.Services.Vehiculo;
using Alquiler_Vehiculos.Services.Vehiculo.Transactions;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Vehiculo;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Services;

/// <summary>
/// Tests unitarios de <see cref="VehiculoService"/>.
/// Se mockean repositorios, caché y el servicio transaccional; los validadores son
/// reales porque son deterministas y forman parte del contrato que el servicio respeta.
/// </summary>
[TestFixture]
[Category("Unit")]
public class VehiculoServiceTests
{
    [TestCase("no-es-una-fecha")]
    [TestCase("9999-01-01")]
    public async Task Create_YUpdate_FechaInvalidaOFutura_Devuelven400SinGuardar(string fecha)
    {
        // Arrange
        var create = CrearCreateValido() with { FechaUltimaRevision = fecha };
        var update = CrearUpdateValido() with { FechaUltimaRevision = fecha };

        // Act
        var alta = await _service.Create(create);
        var modificacion = await _service.Update(update, VehiculoId);

        // Assert
        alta.Error.Should().BeOfType<ValidationError>().Which.ValidationErrors.Should().ContainKey("FechaUltimaRevision");
        modificacion.Error.Should().BeOfType<ValidationError>().Which.ValidationErrors.Should().ContainKey("FechaUltimaRevision");
        var http = Alquiler_Vehiculos.Extensions.DomainErrorExtensions.ToHttpResult(alta.Error);
        http.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>();
        _repository.Verify(r => r.ExistByMatricula(It.IsAny<string>()), Times.Never);
        _repository.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);
        _repository.Verify(r => r.Create(It.IsAny<VehiculoModel>()), Times.Never);
        _fotoRepository.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);
        _transactionsService.Verify(t => t.UpdateAsync(It.IsAny<string>(), It.IsAny<VehiculoModel>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Id no numérico a propósito: las claves de vehículo son string (antes NotFoundVehiculo lanzaba FormatException).
    private const string VehiculoId = "veh-1";
    private const string Matricula = "1234BBB";
    private const string MatriculaNueva = "5678CCC";
    // La foto se referencia por su Nombre, que es la clave de IFotoRepository.
    private const string FotoId = "foto-1";
    private const string ClaveVehiculo = "vehiculo:";
    // C compacto · C 2/4 puertas (Hatchback) · M manual · D diésel con aire.
    private const string AcrissValido = "CCMD";
    // S en 2ª posición no tiene equivalente de dominio: se rechaza antes de consultar dependencias.
    private const string AcrissQueFallaEnElMapper = "ISMD";

    private Mock<IVehiculoRepository> _repository = null!;
    private Mock<IFotoRepository> _fotoRepository = null!;
    private Mock<IVehiculoTransactionsService> _transactionsService = null!;
    private Mock<ICacheService> _cache = null!;
    private VehiculoService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IVehiculoRepository>();
        _fotoRepository = new Mock<IFotoRepository>();
        _transactionsService = new Mock<IVehiculoTransactionsService>();
        _cache = new Mock<ICacheService>();

        // Por defecto no hay nada en caché: cada test que la necesite lo sobreescribe.
        _cache.Setup(c => c.GetAsync<VehiculoDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VehiculoDto?)null);

        _service = new VehiculoService(
            _repository.Object,
            _fotoRepository.Object,
            _transactionsService.Object,
            _cache.Object,
            new CreateVehiculoValidator(),
            new UpdateVehiculoValidator());
    }

    private static CreateVehiculoDto CrearCreateValido() => new()
    {
        Foto = FotoId,
        Matricula = Matricula,
        Marca = "Toyota",
        Modelo = "Corolla",
        Acriss = AcrissValido,
        Color = "Rojo",
        PrecioBaseDia = 50m,
        Plazas = 5,
        MaleteroLitros = 400,
        AutonomiaKm = 600,
        Kilometraje = 1500,
        FechaUltimaRevision = "2025-01-01",
        Concesionario = 1
    };

    private static UpdateVehiculoDto CrearUpdateValido(string matricula = Matricula) => new()
    {
        Foto = FotoId,
        Matricula = matricula,
        Marca = "Toyota",
        Modelo = "Corolla",
        Acriss = AcrissValido,
        Estado = "Disponible",
        Color = "Rojo",
        PrecioBaseDia = 50m,
        Plazas = 5,
        MaleteroLitros = 400,
        AutonomiaKm = 600,
        Kilometraje = 1500,
        FechaUltimaRevision = "2025-01-01",
        Concesionario = 1,
        IsActivo = true
    };

    private static VehiculoModel CrearModelo(string matricula = Matricula, string fotoFk = FotoId) => new()
    {
        Id = VehiculoId,
        FotoFk = fotoFk,
        Foto = new Foto { Nombre = fotoFk, Ruta = fotoFk },
        Matrícula = matricula,
        Marca = "Toyota",
        Modelo = "Corolla",
        Acriss = AcrissValido,
        Carroceria = TipoCarroceria.Hatchback,
        Combustible = TipoCombustible.Diesel,
        Transmision = TipoTransmision.Manual,
        Color = TipoColor.Rojo,
        PrecioBaseDia = 50m,
        Plazas = 5,
        MaleteroLitros = 400,
        AutonomiaKm = 600,
        Kilometraje = 1500,
        FechaUltimaRevision = new DateTime(2025, 1, 1),
        ConesionarioFk = 1,
        Activo = true,
        CreatedAt = new DateTime(2025, 1, 1)
    };

    private static Foto CrearFoto(string id = FotoId) => new()
    {
        Nombre = id,
        Ruta = id,
        Extension = ".jpg",
        Tamano = 1024
    };

    private static VehiculoDto CrearDto(string matricula = Matricula) => new()
    {
        Foto = FotoId,
        Matricula = matricula,
        Marca = "Toyota",
        Modelo = "Corolla",
        Acriss = AcrissValido,
        Categoria = "Hatchback",
        Combustible = "Diesel",
        Transmision = "Manual",
        Estado = "Disponible",
        Color = "Rojo",
        PrecioBaseDia = 50m,
        Plazas = 5,
        MaleteroLitros = 400,
        AutonomiaKm = 600,
        Kilometraje = 1500,
        FechaUltimaRevision = "2025-01-01",
        Concesionario = 1,
        IsActivo = true
    };

    [Test]
    public async Task GetAll_ConVehiculos_DevuelveTodosMapeados()
    {
        // Arrange
        _repository.Setup(r => r.GetAll())
            .ReturnsAsync([CrearModelo(), CrearModelo(MatriculaNueva)]);

        // Act
        var resultado = await _service.GetAll();

        // Assert
        resultado.Should().BeEquivalentTo([CrearDto(), CrearDto(MatriculaNueva)]);

        _repository.Verify(r => r.GetAll(), Times.Once);
    }

    [Test]
    public async Task GetAll_SinVehiculos_DevuelveColeccionVacia()
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
    public async Task GetPaged_ConVehiculos_DevuelveLaPaginaMapeadaYElTotal()
    {
        // Arrange
        _repository.Setup(r => r.GetPaged(2, 1))
            .ReturnsAsync(([CrearModelo(MatriculaNueva)], 2));

        // Act
        var (items, total) = await _service.GetPaged(2, 1);

        // Assert
        (items.ToList(), total).Should().BeEquivalentTo((new List<VehiculoDto> { CrearDto(MatriculaNueva) }, 2));

        _repository.Verify(r => r.GetPaged(2, 1), Times.Once);
        _repository.Verify(r => r.GetAll(), Times.Never);
    }

    [Test]
    public async Task GetPaged_SinVehiculos_DevuelvePaginaVaciaYTotalCero()
    {
        // Arrange
        _repository.Setup(r => r.GetPaged(1, 10))
            .ReturnsAsync((Enumerable.Empty<VehiculoModel>(), 0));

        // Act
        var (items, total) = await _service.GetPaged(1, 10);

        // Assert
        (items.Any(), total).Should().Be((false, 0));

        _repository.Verify(r => r.GetPaged(1, 10), Times.Once);
    }

    [Test]
    public async Task GetById_CuandoEstaEnCache_DevuelveElDtoSinConsultarElRepositorio()
    {
        // Arrange
        _cache.Setup(c => c.GetAsync<VehiculoDto>(ClaveVehiculo + VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CrearDto());

        // Act
        var resultado = await _service.GetById(VehiculoId);

        // Assert
        resultado.Value.Should().BeEquivalentTo(CrearDto());

        _repository.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);
        _cache.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<VehiculoDto>(), It.IsAny<TimeSpan?>(),
                It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task GetById_CuandoNoEstaEnCacheYExiste_GuardaEnCachePorIdYMatricula()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearModelo());

        // Act
        var resultado = await _service.GetById(VehiculoId);

        // Assert
        resultado.Value.Should().BeEquivalentTo(CrearDto());

        _cache.Verify(
            c => c.SetAsync(ClaveVehiculo + VehiculoId, It.IsAny<VehiculoDto>(), It.IsAny<TimeSpan?>(),
                It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _cache.Verify(
            c => c.SetAsync(ClaveVehiculo + Matricula, It.IsAny<VehiculoDto>(), It.IsAny<TimeSpan?>(),
                It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetById_CuandoNoExiste_DevuelveNotFound()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync((VehiculoModel?)null);

        // Act
        var resultado = await _service.GetById(VehiculoId);

        // Assert
        resultado.Error.Should().Be(VehiculoError.NotFoundVehiculo(VehiculoId));

        _cache.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<VehiculoDto>(), It.IsAny<TimeSpan?>(),
                It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Create_CuandoLosDatosNoSonValidos_DevuelveValidationErrorSinConsultarElRepositorio()
    {
        // Arrange
        var dto = CrearCreateValido();
        dto.Matricula = "123";

        // Act
        var resultado = await _service.Create(dto);

        // Assert
        var error = resultado.Error.Should().BeOfType<ValidationError>().Subject;
        error.ValidationErrors.Should().ContainKey("Matricula");

        _repository.Verify(r => r.ExistByMatricula(It.IsAny<string>()), Times.Never);
        _repository.Verify(r => r.Create(It.IsAny<VehiculoModel>()), Times.Never);
    }

    [Test]
    public async Task Create_CuandoLaMatriculaYaExiste_DevuelveConflict()
    {
        // Arrange
        _repository.Setup(r => r.ExistByMatricula(Matricula)).ReturnsAsync(true);

        // Act
        var resultado = await _service.Create(CrearCreateValido());

        // Assert
        resultado.Error.Should().Be(VehiculoError.VehiculoYaExiste(Matricula));

        _fotoRepository.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);
        _repository.Verify(r => r.Create(It.IsAny<VehiculoModel>()), Times.Never);
    }

    [Test]
    public async Task Create_CuandoLaFotoNoExiste_DevuelveNotFound()
    {
        // Arrange
        _repository.Setup(r => r.ExistByMatricula(Matricula)).ReturnsAsync(false);
        _fotoRepository.Setup(r => r.GetById(FotoId)).ReturnsAsync((Foto?)null);

        // Act
        var resultado = await _service.Create(CrearCreateValido());

        // Assert
        resultado.Error.Should().Be(FotoError.FotoNotFound(FotoId));

        _repository.Verify(r => r.Create(It.IsAny<VehiculoModel>()), Times.Never);
    }

    [Test]
    public async Task Create_CuandoElAcrissNoEsMapeable_DevuelveValidacionSinGuardar()
    {
        // Arrange
        var dto = CrearCreateValido();
        dto.Acriss = AcrissQueFallaEnElMapper;
        _repository.Setup(r => r.ExistByMatricula(Matricula)).ReturnsAsync(false);
        _fotoRepository.Setup(r => r.GetById(FotoId)).ReturnsAsync(CrearFoto());

        // Act
        var resultado = await _service.Create(dto);

        // Assert
        resultado.Error.Should().BeOfType<ValidationError>().Which.ValidationErrors.Should().ContainKey("Acriss");
        _repository.Verify(r => r.ExistByMatricula(It.IsAny<string>()), Times.Never);
        _fotoRepository.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);

        _repository.Verify(r => r.Create(It.IsAny<VehiculoModel>()), Times.Never);
    }

    [Test]
    public async Task Create_CuandoTodoEsCorrecto_MapeaYCreaElVehiculo()
    {
        // Arrange
        _repository.Setup(r => r.ExistByMatricula(Matricula)).ReturnsAsync(false);
        _fotoRepository.Setup(r => r.GetById(FotoId)).ReturnsAsync(CrearFoto());
        _repository.Setup(r => r.Create(It.IsAny<VehiculoModel>()))
            .ReturnsAsync((VehiculoModel m) => m);

        // Act
        var resultado = await _service.Create(CrearCreateValido());

        // Assert
        resultado.Value.Matricula.Should().Be(Matricula);

        _repository.Verify(r => r.Create(It.Is<VehiculoModel>(m =>
            m.Matrícula == Matricula
            && m.FotoFk == FotoId
            && m.Marca == "Toyota"
            && m.Carroceria == TipoCarroceria.Hatchback
            && m.Combustible == TipoCombustible.Diesel
            && m.Transmision == TipoTransmision.Manual)), Times.Once);
    }

    [Test]
    public async Task Create_CuandoTodoEsCorrecto_ElVehiculoNaceDisponibleYActivo()
    {
        // Arrange
        _repository.Setup(r => r.ExistByMatricula(Matricula)).ReturnsAsync(false);
        _fotoRepository.Setup(r => r.GetById(FotoId)).ReturnsAsync(CrearFoto());
        _repository.Setup(r => r.Create(It.IsAny<VehiculoModel>()))
            .ReturnsAsync((VehiculoModel m) => m);

        // Act
        var resultado = await _service.Create(CrearCreateValido());

        // Assert
        (resultado.Value.Estado, resultado.Value.IsActivo).Should().Be(("Disponible", true));

        _repository.Verify(r => r.Create(It.Is<VehiculoModel>(m =>
            m.Estado == EstadoVehiculo.Disponible && m.Activo)), Times.Once);
    }

    [Test]
    public async Task Create_CuandoTodoEsCorrecto_GuardaEnCachePorIdYMatricula()
    {
        // Arrange
        _repository.Setup(r => r.ExistByMatricula(Matricula)).ReturnsAsync(false);
        _fotoRepository.Setup(r => r.GetById(FotoId)).ReturnsAsync(CrearFoto());
        _repository.Setup(r => r.Create(It.IsAny<VehiculoModel>()))
            .ReturnsAsync((VehiculoModel m) => m with { Id = VehiculoId });

        // Act
        var resultado = await _service.Create(CrearCreateValido());

        // Assert
        resultado.IsSuccess.Should().BeTrue();

        _cache.Verify(
            c => c.SetAsync(ClaveVehiculo + VehiculoId, It.IsAny<VehiculoDto>(), It.IsAny<TimeSpan?>(),
                It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _cache.Verify(
            c => c.SetAsync(ClaveVehiculo + Matricula, It.IsAny<VehiculoDto>(), It.IsAny<TimeSpan?>(),
                It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Update_CuandoLosDatosNoSonValidos_DevuelveValidationErrorSinConsultarElRepositorio()
    {
        // Arrange
        var dto = CrearUpdateValido();
        dto.Acriss = "ZZ";

        // Act
        var resultado = await _service.Update(dto, VehiculoId);

        // Assert
        var error = resultado.Error.Should().BeOfType<ValidationError>().Subject;
        error.ValidationErrors.Should().ContainKey("Acriss");

        _repository.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);
        _transactionsService.Verify(
            t => t.UpdateAsync(It.IsAny<string>(), It.IsAny<VehiculoModel>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Update_CuandoElVehiculoNoExiste_DevuelveNotFound()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync((VehiculoModel?)null);

        // Act
        var resultado = await _service.Update(CrearUpdateValido(), VehiculoId);

        // Assert
        resultado.Error.Should().Be(VehiculoError.NotFoundVehiculo(VehiculoId));

        _transactionsService.Verify(
            t => t.UpdateAsync(It.IsAny<string>(), It.IsAny<VehiculoModel>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Update_CuandoLaMatriculaCambiaYEstaEnUso_DevuelveConflict()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearModelo());
        _repository.Setup(r => r.ExistByMatricula(MatriculaNueva)).ReturnsAsync(true);

        // Act
        var resultado = await _service.Update(CrearUpdateValido(MatriculaNueva), VehiculoId);

        // Assert
        resultado.Error.Should().Be(VehiculoError.VehiculoYaExiste(MatriculaNueva));

        _transactionsService.Verify(
            t => t.UpdateAsync(It.IsAny<string>(), It.IsAny<VehiculoModel>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Update_CuandoLaMatriculaNoCambia_NoConsultaDuplicados()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearModelo());
        _fotoRepository.Setup(r => r.GetById(FotoId)).ReturnsAsync(CrearFoto());
        _transactionsService
            .Setup(t => t.UpdateAsync(VehiculoId, It.IsAny<VehiculoModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<VehiculoModel, DomainError>(CrearModelo()));

        // Act
        var resultado = await _service.Update(CrearUpdateValido(), VehiculoId);

        // Assert
        resultado.IsSuccess.Should().BeTrue();

        _repository.Verify(r => r.ExistByMatricula(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Update_CuandoLaFotoNoExiste_DevuelveNotFound()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearModelo());
        _fotoRepository.Setup(r => r.GetById(FotoId)).ReturnsAsync((Foto?)null);

        // Act
        var resultado = await _service.Update(CrearUpdateValido(), VehiculoId);

        // Assert
        resultado.Error.Should().Be(FotoError.FotoNotFound(FotoId));

        _transactionsService.Verify(
            t => t.UpdateAsync(It.IsAny<string>(), It.IsAny<VehiculoModel>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Update_CuandoElAcrissNoEsMapeable_DevuelveValidacionSinGuardar()
    {
        // Arrange
        var dto = CrearUpdateValido();
        dto.Acriss = AcrissQueFallaEnElMapper;
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearModelo());
        _fotoRepository.Setup(r => r.GetById(FotoId)).ReturnsAsync(CrearFoto());

        // Act
        var resultado = await _service.Update(dto, VehiculoId);

        // Assert
        resultado.Error.Should().BeOfType<ValidationError>().Which.ValidationErrors.Should().ContainKey("Acriss");
        _repository.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);
        _fotoRepository.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);

        _transactionsService.Verify(
            t => t.UpdateAsync(It.IsAny<string>(), It.IsAny<VehiculoModel>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Update_CuandoLaTransaccionFalla_PropagaElErrorSinTocarLaCache()
    {
        // Arrange
        var error = new BusinessRuleError("ACTUALIZACION_FOTO", "No se pudo actualizar el vehículo: fallo al eliminar la foto anterior.");
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearModelo());
        _fotoRepository.Setup(r => r.GetById(FotoId)).ReturnsAsync(CrearFoto());
        _transactionsService
            .Setup(t => t.UpdateAsync(VehiculoId, It.IsAny<VehiculoModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<VehiculoModel, DomainError>(error));

        // Act
        var resultado = await _service.Update(CrearUpdateValido(), VehiculoId);

        // Assert
        resultado.Error.Should().Be(error);

        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<VehiculoDto>(), It.IsAny<TimeSpan?>(),
                It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Update_CuandoTodoEsCorrecto_ConservaElIdYLaFechaDeCreacion()
    {
        // Arrange
        var existente = CrearModelo();
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(existente);
        _fotoRepository.Setup(r => r.GetById(FotoId)).ReturnsAsync(CrearFoto());
        _transactionsService
            .Setup(t => t.UpdateAsync(VehiculoId, It.IsAny<VehiculoModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<VehiculoModel, DomainError>(CrearModelo()));

        // Act
        await _service.Update(CrearUpdateValido(), VehiculoId);

        // Assert
        _transactionsService.Verify(t => t.UpdateAsync(VehiculoId, It.Is<VehiculoModel>(m =>
            m.Id == existente.Id && m.CreatedAt == existente.CreatedAt)), Times.Once);
    }

    [Test]
    public async Task Update_CuandoTodoEsCorrecto_AplicaEstadoMarcaModeloYConcesionarioDelDto()
    {
        // Arrange
        var dto = CrearUpdateValido();
        dto.Estado = "Mantenimiento";
        dto.Marca = "Renault";
        dto.Modelo = "Megane";
        dto.Concesionario = 9;
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearModelo());
        _fotoRepository.Setup(r => r.GetById(FotoId)).ReturnsAsync(CrearFoto());
        _transactionsService
            .Setup(t => t.UpdateAsync(VehiculoId, It.IsAny<VehiculoModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<VehiculoModel, DomainError>(CrearModelo()));

        // Act
        var resultado = await _service.Update(dto, VehiculoId);

        // Assert
        resultado.IsSuccess.Should().BeTrue();

        _transactionsService.Verify(t => t.UpdateAsync(VehiculoId, It.Is<VehiculoModel>(m =>
            m.Estado == EstadoVehiculo.Mantenimiento
            && m.Marca == "Renault"
            && m.Modelo == "Megane"
            && m.ConesionarioFk == 9
            && m.FotoFk == FotoId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Update_ConEstadoRetirado_DevuelveValidationErrorSinTocarLaTransaccion()
    {
        // Arrange
        var dto = CrearUpdateValido();
        dto.Estado = "Retirado";

        // Act
        var resultado = await _service.Update(dto, VehiculoId);

        // Assert
        var error = resultado.Error.Should().BeOfType<ValidationError>().Subject;
        error.ValidationErrors.Should().ContainKey("Estado");

        _transactionsService.Verify(
            t => t.UpdateAsync(It.IsAny<string>(), It.IsAny<VehiculoModel>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Update_CuandoTodoEsCorrecto_InvalidaLaCacheAntiguaYGuardaLaNueva()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearModelo());
        _repository.Setup(r => r.ExistByMatricula(MatriculaNueva)).ReturnsAsync(false);
        _fotoRepository.Setup(r => r.GetById(FotoId)).ReturnsAsync(CrearFoto());
        _transactionsService
            .Setup(t => t.UpdateAsync(VehiculoId, It.IsAny<VehiculoModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<VehiculoModel, DomainError>(CrearModelo(MatriculaNueva)));

        // Act
        var resultado = await _service.Update(CrearUpdateValido(MatriculaNueva), VehiculoId);

        // Assert
        resultado.Value.Matricula.Should().Be(MatriculaNueva);

        _cache.Verify(c => c.RemoveAsync(ClaveVehiculo + VehiculoId, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(ClaveVehiculo + Matricula, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(ClaveVehiculo + MatriculaNueva, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(
            c => c.SetAsync(ClaveVehiculo + VehiculoId, It.IsAny<VehiculoDto>(), It.IsAny<TimeSpan?>(),
                It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _cache.Verify(
            c => c.SetAsync(ClaveVehiculo + MatriculaNueva, It.IsAny<VehiculoDto>(), It.IsAny<TimeSpan?>(),
                It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task SoftDelete_CuandoLaTransaccionFalla_PropagaElErrorSinTocarLaCache()
    {
        // Arrange
        var error = new BusinessRuleError("BORRADO_FOTO", "No se pudo eliminar la foto asociada al vehículo.");
        _transactionsService
            .Setup(t => t.DeleteAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<VehiculoModel, DomainError>(error));

        // Act
        var resultado = await _service.SoftDelete(VehiculoId);

        // Assert
        resultado.Error.Should().Be(error);

        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SoftDelete_CuandoSeBorra_InvalidaLaCachePorIdYMatricula()
    {
        // Arrange
        _transactionsService
            .Setup(t => t.DeleteAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<VehiculoModel, DomainError>(CrearModelo()));

        // Act
        var resultado = await _service.SoftDelete(VehiculoId);

        // Assert
        resultado.Value.Should().BeTrue();

        _cache.Verify(c => c.RemoveAsync(ClaveVehiculo + VehiculoId, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(ClaveVehiculo + Matricula, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Restore_CuandoLaTransaccionFalla_PropagaElErrorSinTocarLaCache()
    {
        // Arrange
        var error = VehiculoError.NotFoundVehiculo(VehiculoId);
        _transactionsService
            .Setup(t => t.RestoreAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<VehiculoModel, DomainError>(error));

        // Act
        var resultado = await _service.Restore(VehiculoId);

        // Assert
        resultado.Error.Should().Be(error);

        _cache.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<VehiculoDto>(), It.IsAny<TimeSpan?>(),
                It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Restore_CuandoTodoEsCorrecto_GuardaElDtoEnCachePorIdYMatricula()
    {
        // Arrange
        _transactionsService
            .Setup(t => t.RestoreAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<VehiculoModel, DomainError>(CrearModelo()));

        // Act
        var resultado = await _service.Restore(VehiculoId);

        // Assert
        resultado.Value.Matricula.Should().Be(Matricula);

        _cache.Verify(
            c => c.SetAsync(ClaveVehiculo + VehiculoId, It.IsAny<VehiculoDto>(), It.IsAny<TimeSpan?>(),
                It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _cache.Verify(
            c => c.SetAsync(ClaveVehiculo + Matricula, It.IsAny<VehiculoDto>(), It.IsAny<TimeSpan?>(),
                It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
