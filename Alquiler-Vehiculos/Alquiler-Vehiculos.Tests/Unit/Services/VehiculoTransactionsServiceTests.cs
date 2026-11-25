using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Storage;
using Alquiler_Vehiculos.Errors.Vehiculo;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;

namespace Alquiler_Vehiculos.Tests.Unit.Services;

[TestFixture]
[Category("Unit")]
public class VehiculoTransactionsServiceTests {
    private const string VehiculoId = "veh-1";
    private const string FotoId = "foto-1.jpg";
    private const string FotoNuevaId = "foto-2.jpg";

    private Mock<IVehiculoRepository> _vehiculos = null!;
    private Mock<IFotoRepository> _fotos = null!;
    private Mock<ITransactionManager> _transactions = null!;
    private VehiculoTransactionsService _service = null!;

    [SetUp]
    public void SetUp() {
        _vehiculos = new Mock<IVehiculoRepository>();
        _fotos = new Mock<IFotoRepository>();
        _transactions = new Mock<ITransactionManager>();
        _transactions.Setup(t => t.ExecuteAsync(
                It.IsAny<Func<Task<Result<UpdateVehiculoResult, DomainError>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result<UpdateVehiculoResult, DomainError>>> operation, CancellationToken _) =>
                operation());
        _transactions.Setup(t => t.ExecuteAsync(
                It.IsAny<Func<Task<Result<VehiculoModel, DomainError>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result<VehiculoModel, DomainError>>> operation, CancellationToken _) =>
                operation());

        _service = new VehiculoTransactionsService(
            _vehiculos.Object,
            _fotos.Object,
            new Mock<IParteTallerService>().Object,
            _transactions.Object,
            NullLogger<VehiculoTransactionsService>.Instance);
    }

    private static VehiculoModel CrearVehiculo(string fotoFk = FotoId, bool activo = true) => new() {
        Id = VehiculoId,
        Matrícula = "1234BCD",
        FotoFk = fotoFk,
        Foto = new Foto { Nombre = fotoFk, Ruta = $"/uploads/vehiculos/{fotoFk}" },
        Activo = activo
    };

    private static VehiculoModel CrearVehiculoRetirado() {
        var vehiculo = CrearVehiculo(activo: false);
        vehiculo.CambiarEstadoMapper(EstadoVehiculo.Retirado);
        return vehiculo;
    }

    [Test]
    public async Task UpdateAsync_VehiculoInexistente_DevuelveNotFoundSinInsertarFoto() {
        _vehiculos.Setup(r => r.GetByIdForUpdate(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VehiculoModel?)null);

        var resultado = await _service.UpdateAsync(VehiculoId, CrearVehiculo(FotoNuevaId));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(VehiculoError.NotFoundVehiculo(VehiculoId));
        _fotos.Verify(r => r.Create(It.IsAny<Foto>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_VehiculoRetirado_DevuelveConflictoSinInsertarFoto() {
        _vehiculos.Setup(r => r.GetByIdForUpdate(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CrearVehiculo(activo: false));

        var resultado = await _service.UpdateAsync(VehiculoId, CrearVehiculo(FotoNuevaId));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(VehiculoError.NoActualizable(VehiculoId));
        _fotos.Verify(r => r.Create(It.IsAny<Foto>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_MatriculaDuplicada_NoInsertaFotoNiModificaVehiculo() {
        var actual = CrearVehiculo();
        var datos = CrearVehiculo(FotoNuevaId) with { Matrícula = "5678FGH" };
        _vehiculos.Setup(r => r.GetByIdForUpdate(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(actual);
        _vehiculos.Setup(r => r.ExistByMatricula(datos.Matrícula)).ReturnsAsync(true);

        var resultado = await _service.UpdateAsync(VehiculoId, datos);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(VehiculoError.VehiculoYaExiste(datos.Matrícula));
        _fotos.Verify(r => r.Create(It.IsAny<Foto>()), Times.Never);
        _vehiculos.Verify(r => r.ApplyChanges(It.IsAny<VehiculoModel>(), It.IsAny<VehiculoModel>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_CambiaLaFoto_InsertaNuevaYDesactivaLaAnteriorCondicionalmente() {
        var actual = CrearVehiculo();
        var fotoNueva = new Foto { Nombre = FotoNuevaId, Ruta = $"/uploads/vehiculos/{FotoNuevaId}" };
        var datos = CrearVehiculo(FotoNuevaId) with { Matrícula = "5678FGH" };
        var actualizado = actual with { FotoFk = FotoNuevaId, Foto = fotoNueva, Matrícula = datos.Matrícula };
        _vehiculos.Setup(r => r.GetByIdForUpdate(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(actual);
        _vehiculos.Setup(r => r.ExistByMatricula(datos.Matrícula)).ReturnsAsync(false);
        _fotos.Setup(r => r.Create(datos.Foto)).ReturnsAsync(fotoNueva);
        _vehiculos.Setup(r => r.ApplyChanges(actual, datos)).Returns(actualizado);
        _fotos.Setup(r => r.SoftDeleteSiNoAsociada(FotoId, VehiculoId)).ReturnsAsync(true);

        var resultado = await _service.UpdateAsync(VehiculoId, datos);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Vehiculo.Should().BeSameAs(actualizado);
        resultado.Value.MatriculaAnterior.Should().Be(actual.Matrícula);
        _fotos.Verify(r => r.Create(datos.Foto), Times.Once);
        _fotos.Verify(r => r.SoftDeleteSiNoAsociada(FotoId, VehiculoId), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_ViolacionDeUnicidadDeMatricula_DevuelveConflicto() {
        var actual = CrearVehiculo();
        var datos = CrearVehiculo(FotoNuevaId) with { Matrícula = "5678FGH" };
        var duplicate = new DbUpdateException("Duplicada", new PostgresException(
            "Duplicada", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation,
            constraintName: "IX_Vehiculos_Matrícula"));
        _vehiculos.Setup(r => r.GetByIdForUpdate(VehiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(actual);
        _vehiculos.Setup(r => r.ExistByMatricula(datos.Matrícula)).ReturnsAsync(false);
        _fotos.Setup(r => r.Create(It.IsAny<Foto>())).ThrowsAsync(duplicate);

        var resultado = await _service.UpdateAsync(VehiculoId, datos);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(VehiculoError.VehiculoYaExiste(datos.Matrícula));
    }

    [Test]
    public async Task RestoreAsync_VehiculoRetirado_RestauraTambienSuFoto() {
        var retirado = CrearVehiculoRetirado();
        var restaurado = CrearVehiculo() with { Activo = true };
        var foto = new Foto { Nombre = FotoId, Activo = true };
        _vehiculos.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(retirado);
        _vehiculos.Setup(r => r.Restore(VehiculoId)).ReturnsAsync(restaurado);
        _fotos.Setup(r => r.Restore(FotoId)).ReturnsAsync(foto);

        var resultado = await _service.RestoreAsync(VehiculoId);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeSameAs(restaurado);
        _fotos.Verify(r => r.Restore(FotoId), Times.Once);
    }

    [Test]
    public async Task RestoreAsync_VehiculoInexistente_DevuelveNotFound() {
        _vehiculos.Setup(r => r.GetById(VehiculoId)).ReturnsAsync((VehiculoModel?)null);

        var resultado = await _service.RestoreAsync(VehiculoId);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(VehiculoError.NotFoundVehiculo(VehiculoId));
        _vehiculos.Verify(r => r.Restore(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task RestoreAsync_FallaAlRestaurarFoto_DevuelveErrorYPermiteRollback() {
        var retirado = CrearVehiculoRetirado();
        var restaurado = CrearVehiculo() with { Activo = true };
        _vehiculos.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(retirado);
        _vehiculos.Setup(r => r.Restore(VehiculoId)).ReturnsAsync(restaurado);
        _fotos.Setup(r => r.Restore(FotoId)).ReturnsAsync((Foto?)null);

        var resultado = await _service.RestoreAsync(VehiculoId);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(FotoError.NoSePudoRestaurar(FotoId));
    }
}
