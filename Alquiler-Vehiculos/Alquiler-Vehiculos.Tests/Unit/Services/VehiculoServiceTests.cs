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
    private const string VehiculoId = "veh-1";
    private const string Matricula = "1234BBB";
    private const string MatriculaNueva = "5678CCC";
    private const string FotoId = "foto-1";
    private const string FotoUrl = "https://cdn.example.com/foto.jpg";
    private const string ClaveVehiculo = "vehiculo:";

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
        Foto = FotoUrl,
        Matricula = Matricula,
        Marca = "Toyota",
        Modelo = "Corolla",
        Acriss = "CCDM",
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

    private static UpdateVehiculoDto CrearUpdateValido(string matricula = Matricula) => new()
    {
        Foto = FotoUrl,
        Matricula = matricula,
        Marca = "Toyota",
        Modelo = "Corolla",
        Acriss = "CCDM",
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

    private static VehiculoModel CrearModelo(
        string matricula = Matricula,
        string fotoFk = FotoId) => new()
    {
        Id = VehiculoId,
        FotoFk = fotoFk,
        Foto = new Foto { Nombre = fotoFk, Ruta = fotoFk },
        Matrícula = matricula,
        Marca = "Toyota",
        Modelo = "Corolla",
        Acriss = "CCDM",
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
        Activo = true
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
        Acriss = "CCDM",
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
        var error = resultado.Error.Should().BeOfType<BadRequestError.ValidationError>().Subject;
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
        _fotoRepository.Setup(r => r.GetById(FotoUrl)).ReturnsAsync((Foto?)null);

        // Act
        var resultado = await _service.Create(CrearCreateValido());

        // Assert
        resultado.Error.Should().Be(StorageError.FotoNoEncontrada(FotoUrl));

        _repository.Verify(r => r.Create(It.IsAny<VehiculoModel>()), Times.Never);
    }

    [Test]
    public async Task Create_CuandoTodoEsCorrecto_CreaElVehiculoYDevuelveElDto()
    {
        // Arrange
        _repository.Setup(r => r.ExistByMatricula(Matricula)).ReturnsAsync(false);
        _fotoRepository.Setup(r => r.GetById(FotoUrl)).ReturnsAsync(CrearFoto());
        _repository.Setup(r => r.Create(It.IsAny<VehiculoModel>()))
            .ReturnsAsync((VehiculoModel m) => m);

        // Act
        var resultado = await _service.Create(CrearCreateValido());

        // Assert
        resultado.Value.Matricula.Should().Be(Matricula);

        _repository.Verify(r => r.Create(It.Is<VehiculoModel>(m =>
            m.Matrícula == Matricula && m.FotoFk == FotoUrl && m.Marca == "Toyota")), Times.Once);
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
        var error = resultado.Error.Should().BeOfType<BadRequestError.ValidationError>().Subject;
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
    public async Task Update_CuandoLaFotoNoExiste_DevuelveNotFound()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearModelo());
        _fotoRepository.Setup(r => r.GetById(FotoUrl)).ReturnsAsync((Foto?)null);

        // Act
        var resultado = await _service.Update(CrearUpdateValido(), VehiculoId);

        // Assert
        resultado.Error.Should().Be(StorageError.FotoNoEncontrada(FotoUrl));

        _transactionsService.Verify(
            t => t.UpdateAsync(It.IsAny<string>(), It.IsAny<VehiculoModel>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Update_CuandoLaTransaccionFalla_PropagaElErrorSinTocarLaCache()
    {
        // Arrange
        var error = new BusinessRuleError("No se pudo eliminar la foto asociada al vehículo");
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearModelo());
        _fotoRepository.Setup(r => r.GetById(FotoUrl)).ReturnsAsync(CrearFoto());
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
    public async Task Update_CuandoTodoEsCorrecto_InvalidaLaCacheAntiguaYGuardaLaNueva()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearModelo());
        _repository.Setup(r => r.ExistByMatricula(MatriculaNueva)).ReturnsAsync(false);
        _fotoRepository.Setup(r => r.GetById(FotoUrl)).ReturnsAsync(CrearFoto());
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
        var error = VehiculoError.EnAlquiler();
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
    public async Task Restore_CuandoTodoEsCorrecto_GuardaElDtoEnCache()
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
