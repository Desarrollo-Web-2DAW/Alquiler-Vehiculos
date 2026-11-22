using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;
using Alquiler_Vehiculos.Services.Common;
using Alquiler_Vehiculos.Services.Vehiculo.Transactions;
using Alquiler_Vehiculos.Services.Vehiculo.Transactions.Common;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

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
    private Mock<ITransactionManager> _transactionManager = null!;
    private VehiculoTransactionsService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IVehiculoRepository>();
        _fotoRepository = new Mock<IFotoRepository>();
        _transactionManager = new Mock<ITransactionManager>();

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

        _repository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_CuandoExisteConFoto_BorraVehiculoYFoto()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo());
        _repository.Setup(r => r.SoftDelete(VehiculoId)).ReturnsAsync(true);
        _fotoRepository.Setup(r => r.SoftDelete(FotoId)).ReturnsAsync(true);

        // Act
        var resultado = await _service.DeleteAsync(VehiculoId);

        // Assert
        resultado.Value.Matrícula.Should().Be("1234BBB");

        _repository.Verify(r => r.SoftDelete(VehiculoId), Times.Once);
        _fotoRepository.Verify(r => r.SoftDelete(FotoId), Times.Once);
        VerificarTransaccionEjecutada();
    }

    [Test]
    public async Task DeleteAsync_CuandoNoTieneFoto_NoTocaElRepositorioDeFotos()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo(fotoFk: string.Empty));
        _repository.Setup(r => r.SoftDelete(VehiculoId)).ReturnsAsync(true);

        // Act
        var resultado = await _service.DeleteAsync(VehiculoId);

        // Assert
        resultado.IsSuccess.Should().BeTrue();

        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_CuandoFallaElBorradoDeLaFoto_DevuelveBusinessRuleError()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo());
        _repository.Setup(r => r.SoftDelete(VehiculoId)).ReturnsAsync(true);
        _fotoRepository.Setup(r => r.SoftDelete(FotoId)).ReturnsAsync(false);

        // Act
        var resultado = await _service.DeleteAsync(VehiculoId);

        // Assert
        resultado.Error.Should().Be(
            new BusinessRuleError("BORRADO_FOTO", "No se pudo eliminar la foto asociada al vehículo."));

        _repository.Verify(r => r.SoftDelete(VehiculoId), Times.Once);
        _fotoRepository.Verify(r => r.SoftDelete(FotoId), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_CuandoElRepositorioNoBorraElVehiculo_DevuelveBusinessRuleError()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo());
        _repository.Setup(r => r.SoftDelete(VehiculoId)).ReturnsAsync(false);

        // Act
        var resultado = await _service.DeleteAsync(VehiculoId);

        // Assert
        resultado.Error.Should().Be(VehiculoError.BorradoError());

        _repository.Verify(r => r.SoftDelete(VehiculoId), Times.Once);
        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
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
    public async Task UpdateAsync_CuandoFallaElBorradoDeLaFotoAnterior_DevuelveBusinessRuleError()
    {
        // Arrange
        var datos = CrearVehiculo(fotoFk: FotoNuevaId);
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo());
        _repository.Setup(r => r.Update(VehiculoId, datos)).ReturnsAsync(datos);
        _fotoRepository.Setup(r => r.SoftDelete(FotoId)).ReturnsAsync(false);

        // Act
        var resultado = await _service.UpdateAsync(VehiculoId, datos);

        // Assert
        resultado.Error.Should().Be(
            new BusinessRuleError("ACTUALIZACION_FOTO", "No se pudo actualizar el vehículo: fallo al eliminar la foto anterior."));

        _fotoRepository.Verify(r => r.SoftDelete(FotoId), Times.Once);
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
        _repository.Setup(r => r.Restore(VehiculoId)).ReturnsAsync((VehiculoModel?)null);

        // Act
        var resultado = await _service.RestoreAsync(VehiculoId);

        // Assert
        resultado.Error.Should().Be(ErrorNoEncontrado());

        _fotoRepository.Verify(r => r.Restore(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task RestoreAsync_CuandoNoTieneFoto_RestauraSoloElVehiculo()
    {
        // Arrange
        _repository.Setup(r => r.Restore(VehiculoId)).ReturnsAsync(CrearVehiculo(fotoFk: string.Empty));

        // Act
        var resultado = await _service.RestoreAsync(VehiculoId);

        // Assert
        resultado.IsSuccess.Should().BeTrue();

        _repository.Verify(r => r.Restore(VehiculoId), Times.Once);
        _fotoRepository.Verify(r => r.Restore(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task RestoreAsync_CuandoLaFotoNoSeRestaura_DevuelveBusinessRuleError()
    {
        // Arrange
        _repository.Setup(r => r.Restore(VehiculoId)).ReturnsAsync(CrearVehiculo());
        _fotoRepository.Setup(r => r.Restore(FotoId)).ReturnsAsync((Foto?)null);

        // Act
        var resultado = await _service.RestoreAsync(VehiculoId);

        // Assert
        resultado.Error.Should().Be(
            new BusinessRuleError("RESTAURACION_FOTO", "No se pudo restaurar la foto asociada al vehículo."));

        _fotoRepository.Verify(r => r.Restore(FotoId), Times.Once);
    }

    [Test]
    public async Task RestoreAsync_CuandoTodoVaBien_RestauraVehiculoYFoto()
    {
        // Arrange
        var restaurado = CrearVehiculo();
        _repository.Setup(r => r.Restore(VehiculoId)).ReturnsAsync(restaurado);
        _fotoRepository.Setup(r => r.Restore(FotoId)).ReturnsAsync(new Foto { Nombre = FotoId, Ruta = FotoId });

        // Act
        var resultado = await _service.RestoreAsync(VehiculoId);

        // Assert
        resultado.Value.Should().BeSameAs(restaurado);

        _fotoRepository.Verify(r => r.Restore(FotoId), Times.Once);
        VerificarTransaccionEjecutada();
    }
}
