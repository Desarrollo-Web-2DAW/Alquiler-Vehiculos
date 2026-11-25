using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;
using Alquiler_Vehiculos.Services.Vehiculo.Transactions.Common;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Alquiler_Vehiculos.Tests.Integration.Repositories;

public partial class VehiculoRepositoryTests {
    [TestCase(EstadoVehiculo.Disponible)]
    [TestCase(EstadoVehiculo.Reservado)]
    [TestCase(EstadoVehiculo.EnAlquiler)]
    [TestCase(EstadoVehiculo.Mantenimiento)]
    [TestCase(EstadoVehiculo.Retirado)]
    public async Task SoftDelete_EstadoRealEnBaseDeDatos_SoloDaDeBajaDisponible(EstadoVehiculo estado) {
        // Arrange
        var original = await GuardarVehiculo();
        var activoAntes = estado != EstadoVehiculo.Retirado;
        await using var db = CrearContexto();
        await db.Vehiculos.Where(v => v.Id == original.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(v => v.Estado, estado)
                .SetProperty(v => v.Activo, activoAntes));
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var resultado = await repository.SoftDelete(original.Id);

        // Assert
        var eliminado = estado == EstadoVehiculo.Disponible;
        resultado.Should().Be(eliminado);
        await using var lectura = CrearContexto();
        var guardado = await lectura.Vehiculos.SingleAsync();
        guardado.Activo.Should().Be(eliminado ? false : activoAntes);
        guardado.Estado.Should().Be(eliminado ? EstadoVehiculo.Retirado : estado);
        guardado.DeletedAt.HasValue.Should().Be(eliminado);
        (await lectura.Fotos.SingleAsync()).Activo.Should().BeTrue();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ExecuteAsync_ResultadoDominio_ConfirmaORevierteLaBaja(bool exito) {
        // Arrange
        var original = await GuardarVehiculo();
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);
        var manager = new EfTransactionManager(db, NullLogger<EfTransactionManager>.Instance);
        var error = new ConflictError("CONFLICTO_PRUEBA", "La segunda operación rechaza el cambio.");

        // Act
        var resultado = await manager.ExecuteAsync<VehiculoModel>(async () => {
            await repository.SoftDelete(original.Id);
            return exito ? Result.Success<VehiculoModel, DomainError>(original)
                : Result.Failure<VehiculoModel, DomainError>(error);
        });

        // Assert
        resultado.IsSuccess.Should().Be(exito);
        if (!exito) resultado.Error.Should().Be(error);
        await using var lectura = CrearContexto();
        var guardado = await lectura.Vehiculos.SingleAsync();
        guardado.Activo.Should().Be(!exito);
        guardado.Estado.Should().Be(exito ? EstadoVehiculo.Retirado : EstadoVehiculo.Disponible);
        guardado.DeletedAt.HasValue.Should().Be(exito);
    }

    [Test]
    public async Task ExecuteAsync_ExcepcionTrasEscribir_RevierteYPropagaLaExcepcion() {
        // Arrange
        var original = await GuardarVehiculo();
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);
        var manager = new EfTransactionManager(db, NullLogger<EfTransactionManager>.Instance);

        // Act
        Func<Task> act = () => manager.ExecuteAsync<VehiculoModel>(async () => {
            await repository.SoftDelete(original.Id);
            throw new InvalidOperationException("Fallo posterior a la escritura");
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Fallo posterior a la escritura");
        await using var lectura = CrearContexto();
        var guardado = await lectura.Vehiculos.SingleAsync();
        guardado.Activo.Should().BeTrue();
        guardado.Estado.Should().Be(EstadoVehiculo.Disponible);
        guardado.DeletedAt.Should().BeNull();
    }

    [Test]
    public async Task ExecuteAsync_OperacionNula_LanzaArgumentNullException() {
        // Arrange
        await using var db = CrearContexto();
        var manager = new EfTransactionManager(db, NullLogger<EfTransactionManager>.Instance);

        // Act
        Func<Task> act = () => manager.ExecuteAsync<VehiculoModel>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
        db.Database.CurrentTransaction.Should().BeNull();
    }

    [Test]
    public async Task ExecuteAsync_TransaccionAmbiente_LaReutilizaSinConfirmarPorSuCuenta() {
        // Arrange
        var original = await GuardarVehiculo();
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);
        var manager = new EfTransactionManager(db, NullLogger<EfTransactionManager>.Instance);
        await using var ambiente = await db.Database.BeginTransactionAsync();

        // Act
        var resultado = await manager.ExecuteAsync<VehiculoModel>(async () => {
            await repository.SoftDelete(original.Id);
            return Result.Success<VehiculoModel, DomainError>(original);
        });

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        db.Database.CurrentTransaction.Should().BeSameAs(ambiente);
        await ambiente.RollbackAsync();
        await using var lectura = CrearContexto();
        var guardado = await lectura.Vehiculos.SingleAsync();
        guardado.Activo.Should().BeTrue();
        guardado.DeletedAt.Should().BeNull();
    }

    [Test]
    public async Task ExecuteAsync_FalloAlRevertir_ConservaLaExcepcionOriginal() {
        // Arrange
        await using var db = CrearContexto();
        var manager = new EfTransactionManager(db, NullLogger<EfTransactionManager>.Instance);

        // Act
        Func<Task> act = () => manager.ExecuteAsync<VehiculoModel>(async () => {
            // La transacción queda cerrada antes del catch, así que el reintento de
            // reversión falla y se registra sin ocultar la excepción que provocó el fallo.
            await db.Database.CurrentTransaction!.RollbackAsync();
            throw new InvalidOperationException("Fallo tras revertir la transacción");
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Fallo tras revertir la transacción");
        db.Database.CurrentTransaction.Should().BeNull();
    }
}
