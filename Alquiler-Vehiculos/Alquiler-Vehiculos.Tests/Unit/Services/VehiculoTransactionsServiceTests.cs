using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Errors.Storage;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;
using Alquiler_Vehiculos.Services.Common;
using Alquiler_Vehiculos.Services.Taller;
using Alquiler_Vehiculos.Services.Vehiculo.Transactions;
using Alquiler_Vehiculos.Services.Vehiculo.Transactions.Common;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Alquiler_Vehiculos.Tests.Unit.Services;

/// <summary>
/// Tests unitarios de <see cref="VehiculoTransactionsService"/>.
/// Se mockean los dos repositorios y el gestor de transacciones se sustituye por un
/// doble que ejecuta la operación tal cual: así el test es rápido, independiente y
/// no necesita PostgreSQL. El doble también permite comprobar el token que recibe.
/// </summary>
[TestFixture]
[Category("Unit")]
public class VehiculoTransactionsServiceTests
{
    private const string VehiculoId = "veh-1";
    private const string FotoId = "foto-1";
    private const string FotoNuevaId = "foto-2";

    private Mock<IVehiculoRepository> _repository = null!;
    private Mock<IFotoRepository> _fotoRepository = null!;
    private Mock<IParteTallerService> _parteTallerService = null!;
    private Mock<ITransactionManager> _transactionManager = null!;
    private VehiculoTransactionsService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IVehiculoRepository>();
        _fotoRepository = new Mock<IFotoRepository>();
        _parteTallerService = new Mock<IParteTallerService>();
        _transactionManager = new Mock<ITransactionManager>();

        // Por defecto el vehículo no tiene partes abiertos; cada test que lo necesite lo cambia.
        _parteTallerService
            .Setup(p => p.TienePartesAbiertosAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<bool, DomainError>(false));

        // El gestor de transacciones es una frontera: en el test se limita a
        // ejecutar la operación sin abrir transacción real.
        _transactionManager
            .Setup(t => t.ExecuteAsync(
                It.IsAny<Func<Task<Result<VehiculoModel, DomainError>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result<VehiculoModel, DomainError>>> operacion, CancellationToken _) => operacion());

        _service = new VehiculoTransactionsService(
            _repository.Object,
            _fotoRepository.Object,
            _parteTallerService.Object,
            _transactionManager.Object,
            NullLogger<VehiculoTransactionsService>.Instance);
    }

    private static VehiculoModel CrearVehiculo(string fotoFk = FotoId) => new()
    {
        Id = VehiculoId,
        Matrícula = "1234BBB",
        FotoFk = fotoFk,
        Foto = new Foto { Nombre = fotoFk, Ruta = fotoFk }
    };

    private static VehiculoModel CrearVehiculoRetirado(string fotoFk = FotoId)
    {
        var vehiculo = CrearVehiculo(fotoFk) with { Activo = false };
        vehiculo.CambiarEstadoMapper(EstadoVehiculo.Retirado);
        return vehiculo;
    }

    // Id no numérico a propósito: antes NotFoundVehiculo hacía Convert.ToInt64 y lanzaba FormatException.
    private static NotFoundError ErrorNoEncontrado() => VehiculoError.NotFoundVehiculo(VehiculoId);

    private void VerificarTransaccionEjecutada(CancellationToken ct = default) =>
        _transactionManager.Verify(
            t => t.ExecuteAsync(
                It.IsAny<Func<Task<Result<VehiculoModel, DomainError>>>>(), ct),
            Times.Once);

    [Test]
    public async Task DeleteAsync_CuandoElVehiculoNoExiste_DevuelveNotFound()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync((VehiculoModel?)null);

        // Act
        var resultado = await _service.DeleteAsync(VehiculoId);

        // Assert
        resultado.Error.Should().Be(ErrorNoEncontrado());

        _parteTallerService.Verify(
            p => p.TienePartesAbiertosAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        VerificarTransaccionEjecutada();
    }

    [TestCase(EstadoVehiculo.Reservado)]
    [TestCase(EstadoVehiculo.EnAlquiler)]
    [TestCase(EstadoVehiculo.Mantenimiento)]
    [TestCase(EstadoVehiculo.Retirado)]
    public async Task DeleteAsync_CuandoNoEstaDisponible_DevuelveConflictoSinBorrar(EstadoVehiculo estado)
    {
        // Arrange
        var vehiculo = CrearVehiculo();
        vehiculo.CambiarEstadoMapper(estado);
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(vehiculo);

        // Act
        var resultado = await _service.DeleteAsync(VehiculoId);

        // Assert
        resultado.Error.Should().Be(VehiculoError.NoDisponibleParaBaja(estado));

        _parteTallerService.Verify(
            p => p.TienePartesAbiertosAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_CuandoTieneUnParteAbierto_DevuelveConflictoSinBorrar()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo());
        _parteTallerService
            .Setup(p => p.TienePartesAbiertosAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<bool, DomainError>(true));

        // Act
        var resultado = await _service.DeleteAsync(VehiculoId);

        // Assert — RF-01 y RF-08: con un parte abierto no se da de baja (409) ni se borra nada.
        resultado.Error.Should().Be(VehiculoError.VehiculoConPartesAbiertos(VehiculoId));

        _parteTallerService.Verify(
            p => p.TienePartesAbiertosAsync(VehiculoId, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_CuandoLaConsultaDePartesFalla_DevuelveElErrorSinBorrar()
    {
        // Arrange
        var error = new ConflictError("PARTES_NO_DISPONIBLES", "No se pudo consultar el taller.");
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo());
        _parteTallerService
            .Setup(p => p.TienePartesAbiertosAsync(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool, DomainError>(error));

        // Act
        var resultado = await _service.DeleteAsync(VehiculoId);

        // Assert
        resultado.Error.Should().Be(error);

        _repository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_CuandoExisteConFoto_DaDeBajaElVehiculoYConservaLaFoto()
    {
        // Arrange
        var vehiculo = CrearVehiculo();
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(vehiculo);
        _repository.Setup(r => r.SoftDeleteSiEstado(VehiculoId, EstadoVehiculo.Disponible)).ReturnsAsync(true);

        // Act
        var resultado = await _service.DeleteAsync(VehiculoId);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.FotoFk.Should().Be(FotoId);
        resultado.Value.Foto.Should().BeSameAs(vehiculo.Foto);
        vehiculo.Foto.Activo.Should().BeTrue();
        vehiculo.Foto.DeletedAt.Should().BeNull();

        _repository.Verify(r => r.GetById(VehiculoId), Times.Once);
        _parteTallerService.Verify(
            p => p.TienePartesAbiertosAsync(VehiculoId, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SoftDeleteSiEstado(VehiculoId, EstadoVehiculo.Disponible), Times.Once);
        _repository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        VerificarTransaccionEjecutada();
    }

    [Test]
    public async Task DeleteAsync_CuandoNoTieneFoto_NoTocaElRepositorioDeFotos()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo(fotoFk: string.Empty));
        _repository.Setup(r => r.SoftDeleteSiEstado(VehiculoId, EstadoVehiculo.Disponible)).ReturnsAsync(true);

        // Act
        var resultado = await _service.DeleteAsync(VehiculoId);

        // Assert
        resultado.IsSuccess.Should().BeTrue();

        _repository.Verify(r => r.GetById(VehiculoId), Times.Once);
        _repository.Verify(r => r.SoftDeleteSiEstado(VehiculoId, EstadoVehiculo.Disponible), Times.Once);
        _parteTallerService.Verify(p => p.TienePartesAbiertosAsync(VehiculoId, It.IsAny<CancellationToken>()), Times.Once);
        VerificarTransaccionEjecutada();
        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_SinFoto_ConservaLasComprobacionesDeBaja()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo(string.Empty));
        _repository.Setup(r => r.SoftDeleteSiEstado(VehiculoId, EstadoVehiculo.Disponible)).ReturnsAsync(true);

        // Act
        var resultado = await _service.DeleteAsync(VehiculoId);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        _repository.Verify(r => r.GetById(VehiculoId), Times.Once);
        _parteTallerService.Verify(p => p.TienePartesAbiertosAsync(VehiculoId, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SoftDeleteSiEstado(VehiculoId, EstadoVehiculo.Disponible), Times.Once);
        _repository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        VerificarTransaccionEjecutada();
    }

    [Test]
    public async Task DeleteAsync_CuandoElVehiculoDesapareceAntesDeBorrar_DevuelveNotFound()
    {
        // Arrange
        _repository.SetupSequence(r => r.GetById(VehiculoId))
            .ReturnsAsync(CrearVehiculo()).ReturnsAsync((VehiculoModel?)null);
        _repository.Setup(r => r.SoftDeleteSiEstado(VehiculoId, EstadoVehiculo.Disponible)).ReturnsAsync(false);

        // Act
        var resultado = await _service.DeleteAsync(VehiculoId);

        // Assert
        resultado.Error.Should().Be(ErrorNoEncontrado());

        _repository.Verify(r => r.GetById(VehiculoId), Times.Exactly(2));
        _parteTallerService.Verify(p => p.TienePartesAbiertosAsync(VehiculoId, It.IsAny<CancellationToken>()), Times.Once);
        VerificarTransaccionEjecutada();

        _repository.Verify(r => r.SoftDeleteSiEstado(VehiculoId, EstadoVehiculo.Disponible), Times.Once);
        _repository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
    }

    [TestCase(EstadoVehiculo.Reservado)]
    [TestCase(EstadoVehiculo.EnAlquiler)]
    [TestCase(EstadoVehiculo.Mantenimiento)]
    public async Task DeleteAsync_CambioConcurrenteDeEstado_Devuelve409SinBaja(EstadoVehiculo estado) {
        // Arrange
        var ultimo = CrearVehiculo();
        ultimo.CambiarEstadoMapper(estado);
        _repository.SetupSequence(r => r.GetById(VehiculoId))
            .ReturnsAsync(CrearVehiculo()).ReturnsAsync(ultimo);
        _repository.Setup(r => r.SoftDeleteSiEstado(VehiculoId, EstadoVehiculo.Disponible)).ReturnsAsync(false);

        // Act
        var resultado = await _service.DeleteAsync(VehiculoId);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(VehiculoError.NoDisponibleParaBaja(estado));
        _repository.Verify(r => r.GetById(VehiculoId), Times.Exactly(2));
        _repository.Verify(r => r.SoftDeleteSiEstado(VehiculoId, EstadoVehiculo.Disponible), Times.Once);
        _repository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        _parteTallerService.Verify(p => p.TienePartesAbiertosAsync(VehiculoId, It.IsAny<CancellationToken>()), Times.Once);
        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        VerificarTransaccionEjecutada();
    }

    [Test]
    public async Task DeleteAsync_ConTokenDeCancelacion_LoPropagaAlGestorDeTransacciones()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo(fotoFk: string.Empty));

        // Act
        await _service.DeleteAsync(VehiculoId, cts.Token);

        // Assert
        VerificarTransaccionEjecutada(cts.Token);
    }

    [Test]
    public async Task UpdateAsync_CuandoElVehiculoNoExiste_DevuelveNotFound()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync((VehiculoModel?)null);

        // Act
        var resultado = await _service.UpdateAsync(VehiculoId, CrearVehiculo());

        // Assert
        resultado.Error.Should().Be(ErrorNoEncontrado());

        _repository.Verify(r => r.Update(It.IsAny<string>(), It.IsAny<VehiculoModel>()), Times.Never);
        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_CuandoElRepositorioDevuelveNull_DevuelveNotFound()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo());
        _repository.Setup(r => r.Update(VehiculoId, It.IsAny<VehiculoModel>()))
            .ReturnsAsync((VehiculoModel?)null);

        // Act
        var resultado = await _service.UpdateAsync(VehiculoId, CrearVehiculo());

        // Assert
        resultado.Error.Should().Be(ErrorNoEncontrado());

        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_CuandoLaFotoCambia_EliminaLaFotoAnterior()
    {
        // Arrange
        var datos = CrearVehiculo(fotoFk: FotoNuevaId);
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo());
        _repository.Setup(r => r.Update(VehiculoId, datos)).ReturnsAsync(datos);
        _fotoRepository.Setup(r => r.SoftDelete(FotoId)).ReturnsAsync(true);

        // Act
        var resultado = await _service.UpdateAsync(VehiculoId, datos);

        // Assert
        resultado.Value.FotoFk.Should().Be(FotoNuevaId);

        _fotoRepository.Verify(r => r.SoftDelete(FotoId), Times.Once);
        _fotoRepository.Verify(r => r.SoftDelete(FotoNuevaId), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_CuandoLaFotoNoCambia_NoTocaElRepositorioDeFotos()
    {
        // Arrange
        var datos = CrearVehiculo();
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo());
        _repository.Setup(r => r.Update(VehiculoId, datos)).ReturnsAsync(datos);

        // Act
        var resultado = await _service.UpdateAsync(VehiculoId, datos);

        // Assert
        resultado.IsSuccess.Should().BeTrue();

        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_CuandoElVehiculoNoTeniaFoto_NoBorraNingunaFoto()
    {
        // Arrange
        var datos = CrearVehiculo(fotoFk: FotoNuevaId);
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo(fotoFk: string.Empty));
        _repository.Setup(r => r.Update(VehiculoId, datos)).ReturnsAsync(datos);

        // Act
        var resultado = await _service.UpdateAsync(VehiculoId, datos);

        // Assert
        resultado.IsSuccess.Should().BeTrue();

        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_CuandoFallaElBorradoDeLaFotoAnterior_DevuelveInternalError()
    {
        // Arrange
        var datos = CrearVehiculo(fotoFk: FotoNuevaId);
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo());
        _repository.Setup(r => r.Update(VehiculoId, datos)).ReturnsAsync(datos);
        _fotoRepository.Setup(r => r.SoftDelete(FotoId)).ReturnsAsync(false);
        _fotoRepository.Setup(r => r.GetById(FotoId)).ReturnsAsync(new Foto { Nombre = FotoId, Activo = true });

        // Act
        var resultado = await _service.UpdateAsync(VehiculoId, datos);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(FotoError.NoSePudoDarDeBaja(FotoId));

        _repository.Verify(r => r.GetById(VehiculoId), Times.Once);
        _repository.Verify(r => r.Update(VehiculoId, datos), Times.Once);
        VerificarTransaccionEjecutada();

        _fotoRepository.Verify(r => r.SoftDelete(FotoId), Times.Once);
        _fotoRepository.Verify(r => r.GetById(FotoId), Times.Once);
    }

    [TestCase("ausente")]
    [TestCase("inactiva")]
    [TestCase("borrada")]
    public async Task UpdateAsync_FotoAnteriorYaDeBaja_ConservaExito(string estado) {
        // Arrange
        var datos = CrearVehiculo(FotoNuevaId);
        Foto? anterior = estado == "ausente" ? null : new Foto {
            Nombre = FotoId, Activo = estado != "inactiva",
            DeletedAt = estado == "borrada" ? new DateTime(2026, 1, 1) : null
        };
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo());
        _repository.Setup(r => r.Update(VehiculoId, datos)).ReturnsAsync(datos);
        _fotoRepository.Setup(r => r.SoftDelete(FotoId)).ReturnsAsync(false);
        _fotoRepository.Setup(r => r.GetById(FotoId)).ReturnsAsync(anterior);

        // Act
        var resultado = await _service.UpdateAsync(VehiculoId, datos);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.FotoFk.Should().Be(FotoNuevaId);
        _repository.Verify(r => r.GetById(VehiculoId), Times.Once);
        _repository.Verify(r => r.Update(VehiculoId, datos), Times.Once);
        _fotoRepository.Verify(r => r.SoftDelete(FotoId), Times.Once);
        _fotoRepository.Verify(r => r.GetById(FotoId), Times.Once);
        _parteTallerService.Verify(p => p.TienePartesAbiertosAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        VerificarTransaccionEjecutada();
    }

    [TestCase("IX_Vehiculos_Matrícula", true)]
    [TestCase("OTRA_RESTRICCION", false)]
    public async Task UpdateAsync_RestriccionUnica_SoloTraduceLaMatricula(string restriccion, bool matriculaDuplicada) {
        // Arrange
        var datos = CrearVehiculo(FotoNuevaId);
        var excepcion = new DbUpdateException("Restricción única", new PostgresException(
            "Valor duplicado", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation,
            constraintName: restriccion));
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo());
        _repository.Setup(r => r.Update(VehiculoId, datos)).ThrowsAsync(excepcion);
        Result<VehiculoModel, DomainError>? resultado = null;

        // Act
        Func<Task> act = async () => { resultado = await _service.UpdateAsync(VehiculoId, datos); };

        // Assert
        if (matriculaDuplicada) {
            await act.Should().NotThrowAsync();
            resultado!.Value.IsFailure.Should().BeTrue();
            resultado.Value.Error.Should().Be(VehiculoError.VehiculoYaExiste(datos.Matrícula));
        } else {
            await act.Should().ThrowAsync<DbUpdateException>();
        }
        _repository.Verify(r => r.GetById(VehiculoId), Times.Once);
        _repository.Verify(r => r.Update(VehiculoId, datos), Times.Once);
        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        _parteTallerService.Verify(p => p.TienePartesAbiertosAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        VerificarTransaccionEjecutada();
    }

    [Test]
    public async Task UpdateAsync_ConTokenDeCancelacion_LoPropagaAlGestorDeTransacciones()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var datos = CrearVehiculo();
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo());
        _repository.Setup(r => r.Update(VehiculoId, datos)).ReturnsAsync(datos);

        // Act
        await _service.UpdateAsync(VehiculoId, datos, cts.Token);

        // Assert
        VerificarTransaccionEjecutada(cts.Token);
    }

    [Test]
    public async Task RestoreAsync_CuandoElVehiculoNoExiste_DevuelveNotFound()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync((VehiculoModel?)null);

        // Act
        var resultado = await _service.RestoreAsync(VehiculoId);

        // Assert
        resultado.Error.Should().Be(ErrorNoEncontrado());

        _repository.Verify(r => r.GetById(VehiculoId), Times.Once);
        _repository.Verify(r => r.Restore(It.IsAny<string>()), Times.Never);
        _fotoRepository.Verify(r => r.Restore(It.IsAny<string>()), Times.Never);
        VerificarTransaccionEjecutada();
    }

    [TestCase(EstadoVehiculo.Disponible)]
    [TestCase(EstadoVehiculo.Reservado)]
    [TestCase(EstadoVehiculo.EnAlquiler)]
    [TestCase(EstadoVehiculo.Mantenimiento)]
    public async Task RestoreAsync_CuandoNoEstaRetirado_DevuelveConflictoSinRestaurar(EstadoVehiculo estado)
    {
        // Arrange
        var vehiculo = CrearVehiculo();
        vehiculo.CambiarEstadoMapper(estado);
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(vehiculo);

        // Act
        var resultado = await _service.RestoreAsync(VehiculoId);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(VehiculoError.NoRetiradoParaRestaurar(estado));

        _repository.Verify(r => r.Restore(It.IsAny<string>()), Times.Never);
        _fotoRepository.Verify(r => r.Restore(It.IsAny<string>()), Times.Never);
        VerificarTransaccionEjecutada();
    }

    [Test]
    public async Task RestoreAsync_CuandoYaEstaActivoAunqueEsteRetirado_DevuelveConflictoSinRestaurar()
    {
        // Arrange
        var vehiculo = CrearVehiculoRetirado() with { Activo = true };
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(vehiculo);

        // Act
        var resultado = await _service.RestoreAsync(VehiculoId);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(VehiculoError.NoRetiradoParaRestaurar(EstadoVehiculo.Retirado));

        _repository.Verify(r => r.Restore(It.IsAny<string>()), Times.Never);
        _fotoRepository.Verify(r => r.Restore(It.IsAny<string>()), Times.Never);
        VerificarTransaccionEjecutada();
    }

    [Test]
    public async Task RestoreAsync_CuandoOtroProcesoCambiaElEstadoAntesDeRestaurar_DevuelveConflicto()
    {
        // Arrange
        var retirado = CrearVehiculoRetirado();
        var alquilado = CrearVehiculo();
        alquilado.CambiarEstadoMapper(EstadoVehiculo.EnAlquiler);
        _repository.SetupSequence(r => r.GetById(VehiculoId))
            .ReturnsAsync(retirado)
            .ReturnsAsync(alquilado);
        _repository.Setup(r => r.Restore(VehiculoId)).ReturnsAsync((VehiculoModel?)null);

        // Act
        var resultado = await _service.RestoreAsync(VehiculoId);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(VehiculoError.NoRetiradoParaRestaurar(EstadoVehiculo.EnAlquiler));

        _repository.Verify(r => r.Restore(VehiculoId), Times.Once);
        _fotoRepository.Verify(r => r.Restore(It.IsAny<string>()), Times.Never);
        VerificarTransaccionEjecutada();
    }

    [Test]
    public async Task RestoreAsync_CuandoNoTieneFoto_RestauraSoloElVehiculo()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculoRetirado(string.Empty));
        _repository.Setup(r => r.Restore(VehiculoId)).ReturnsAsync(CrearVehiculo(fotoFk: string.Empty));

        // Act
        var resultado = await _service.RestoreAsync(VehiculoId);

        // Assert
        resultado.IsSuccess.Should().BeTrue();

        _repository.Verify(r => r.Restore(VehiculoId), Times.Once);
        _fotoRepository.Verify(r => r.Restore(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task RestoreAsync_CuandoLaFotoNoSeRestaura_DevuelveInternalError()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculoRetirado());
        _repository.Setup(r => r.Restore(VehiculoId)).ReturnsAsync(CrearVehiculo());
        _fotoRepository.Setup(r => r.Restore(FotoId)).ReturnsAsync((Foto?)null);

        // Act
        var resultado = await _service.RestoreAsync(VehiculoId);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(FotoError.NoSePudoRestaurar(FotoId));

        _repository.Verify(r => r.Restore(VehiculoId), Times.Once);
        VerificarTransaccionEjecutada();

        _fotoRepository.Verify(r => r.Restore(FotoId), Times.Once);
    }

    [Test]
    public async Task RestoreAsync_CuandoTodoVaBien_RestauraVehiculoYFoto()
    {
        // Arrange
        var restaurado = CrearVehiculo();
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculoRetirado());
        _repository.Setup(r => r.Restore(VehiculoId)).ReturnsAsync(restaurado);
        _fotoRepository.Setup(r => r.Restore(FotoId)).ReturnsAsync(new Foto { Nombre = FotoId, Ruta = FotoId });

        // Act
        var resultado = await _service.RestoreAsync(VehiculoId);

        // Assert
        resultado.Value.Should().BeSameAs(restaurado);

        _repository.Verify(r => r.Restore(VehiculoId), Times.Once);
        _fotoRepository.Verify(r => r.Restore(FotoId), Times.Once);
        VerificarTransaccionEjecutada();
    }
}
