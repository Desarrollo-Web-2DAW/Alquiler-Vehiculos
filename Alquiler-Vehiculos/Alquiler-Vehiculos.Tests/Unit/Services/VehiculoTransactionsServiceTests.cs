using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;
using Alquiler_Vehiculos.Services.Common;
using Alquiler_Vehiculos.Services.Vehiculo;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Services;

/// <summary>
/// Tests unitarios de <see cref="VehiculoTransactionsService"/>.
/// Se mockean los dos repositorios y se sustituye el gestor de transacciones por un
/// doble que ejecuta la operación sin abrir transacción real: así el test es rápido,
/// independiente y no necesita PostgreSQL.
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
    private FakeTransactionManager _transactionManager = null!;
    private VehiculoTransactionsService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IVehiculoRepository>();
        _fotoRepository = new Mock<IFotoRepository>();
        _transactionManager = new FakeTransactionManager();

        _service = new VehiculoTransactionsService(
            _repository.Object,
            _fotoRepository.Object,
            _transactionManager);
    }

    private static VehiculoModel CrearVehiculo(
        string fotoFk = FotoId,
        EstadoVehiculo estado = EstadoVehiculo.Disponible)
    {
        var vehiculo = new VehiculoModel
        {
            Id = VehiculoId,
            Matrícula = "1234BBB",
            FotoFk = fotoFk,
            Foto = new Foto { Nombre = fotoFk, Ruta = fotoFk }
        };

        vehiculo.CambiarEstadoMapper(estado);
        return vehiculo;
    }

    [Test]
    public async Task DeleteAsync_CuandoElVehiculoNoExiste_DevuelveNotFound()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync((VehiculoModel?)null);

        // Act
        var resultado = await _service.DeleteAsync(VehiculoId);

        // Assert
        resultado.Error.Should().Be(VehiculoError.NotFoundVehiculo(VehiculoId));

        _repository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_CuandoElVehiculoEstaAlquilado_DevuelveBusinessRuleError()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId))
            .ReturnsAsync(CrearVehiculo(estado: EstadoVehiculo.EnAlquiler));

        // Act
        var resultado = await _service.DeleteAsync(VehiculoId);

        // Assert
        resultado.Error.Should().Be(VehiculoError.EnAlquiler());

        _repository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_CuandoExisteYBorraVehiculoYFoto_DevuelveElVehiculoEliminado()
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
        _transactionManager.Llamadas.Should().Be(1);
    }

    [Test]
    public async Task DeleteAsync_CuandoElVehiculoNoTieneFoto_NoTocaElRepositorioDeFotos()
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
        resultado.Error.Should().Be(VehiculoError.FotoAsociadaNoEliminada());

        _fotoRepository.Verify(r => r.SoftDelete(FotoId), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_CuandoFallaElBorradoDelVehiculo_DevuelveBusinessRuleError()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearVehiculo());
        _repository.Setup(r => r.SoftDelete(VehiculoId)).ReturnsAsync(false);

        // Act
        var resultado = await _service.DeleteAsync(VehiculoId);

        // Assert
        resultado.Error.Should().Be(VehiculoError.BorradoError());

        _fotoRepository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_CuandoElVehiculoNoExiste_DevuelveNotFound()
    {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync((VehiculoModel?)null);

        // Act
        var resultado = await _service.UpdateAsync(VehiculoId, CrearVehiculo());

        // Assert
        resultado.Error.Should().Be(VehiculoError.NotFoundVehiculo(VehiculoId));

        _repository.Verify(r => r.Update(It.IsAny<string>(), It.IsAny<VehiculoModel>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_CuandoCambiaLaFoto_EliminaLaFotoAnterior()
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
        resultado.Error.Should().Be(VehiculoError.FotoAsociadaNoEliminada());

        _fotoRepository.Verify(r => r.SoftDelete(FotoId), Times.Once);
    }

    [Test]
    public async Task RestoreAsync_CuandoElVehiculoNoExiste_DevuelveNotFound()
    {
        // Arrange
        _repository.Setup(r => r.Restore(VehiculoId)).ReturnsAsync((VehiculoModel?)null);

        // Act
        var resultado = await _service.RestoreAsync(VehiculoId);

        // Assert
        resultado.Error.Should().Be(VehiculoError.NotFoundVehiculo(VehiculoId));

        _fotoRepository.Verify(r => r.Restore(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task RestoreAsync_CuandoExisteYNoTieneFoto_RestauraSoloElVehiculo()
    {
        // Arrange
        _repository.Setup(r => r.Restore(VehiculoId))
            .ReturnsAsync(CrearVehiculo(fotoFk: string.Empty));

        // Act
        var resultado = await _service.RestoreAsync(VehiculoId);

        // Assert
        resultado.IsSuccess.Should().BeTrue();

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
        resultado.Error.Should().Be(VehiculoError.FotoAsociadaNoRestaurada());

        _fotoRepository.Verify(r => r.Restore(FotoId), Times.Once);
    }

    /// <summary>
    /// Doble de <see cref="ITransactionManager"/> que ejecuta la operación sin
    /// abrir transacción real y cuenta las invocaciones.
    /// </summary>
    private sealed class FakeTransactionManager : ITransactionManager
    {
        public int Llamadas { get; private set; }

        public Task<Result<T, DomainError>> ExecuteAsync<T>(
            Func<Task<Result<T, DomainError>>> operation,
            CancellationToken ct = default)
        {
            Llamadas++;
            return operation();
        }
    }
}
