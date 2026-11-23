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
    public async Task SoftDeleteSiEstado_EstadoRealEnBaseDeDatos_SoloDaDeBajaDisponible(EstadoVehiculo estado) {
        // Arrange
        var original = await GuardarVehiculo();
        await using var db = CrearContexto();
        await db.Vehiculos.Where(v => v.Id == original.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.Estado, estado));
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var resultado = await repository.SoftDeleteSiEstado(original.Id, EstadoVehiculo.Disponible);

        // Assert
        var eliminado = estado == EstadoVehiculo.Disponible;
        resultado.Should().Be(eliminado);
        await using var lectura = CrearContexto();
        var guardado = await lectura.Vehiculos.SingleAsync();
        guardado.Activo.Should().Be(!eliminado);
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
            await repository.SoftDeleteSiEstado(original.Id, EstadoVehiculo.Disponible);
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
            await repository.SoftDeleteSiEstado(original.Id, EstadoVehiculo.Disponible);
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
}
