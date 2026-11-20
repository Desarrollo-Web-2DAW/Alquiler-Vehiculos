using Alquiler_Vehiculos.Repositories.Vehiculo.Concesionario;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Alquiler_Vehiculos.Tests.Integration.Repositories;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class ConcesionarioRepositoryTests : RepositoryTestBase {
    [Test]
    public async Task Create_ConcesionarioValido_GeneraIdYPersisteDatos() {
        // Arrange
        var concesionario = CrearConcesionario();
        await using var db = CrearContexto();
        var repository = new ConcesionarioRepository(db, NullLogger<ConcesionarioRepository>.Instance);

        // Act
        var resultado = await repository.Create(concesionario);

        // Assert
        resultado.Id.Should().BeGreaterThan(0);
        await using var lectura = CrearContexto();
        (await lectura.Concesionarios.SingleAsync()).Should().BeEquivalentTo(resultado);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetById_ExisteONo_DevuelveResultadoSinTracking(bool existe) {
        // Arrange
        var concesionario = existe ? await GuardarConcesionario() : CrearConcesionario();
        await using var db = CrearContexto();
        var repository = new ConcesionarioRepository(db, NullLogger<ConcesionarioRepository>.Instance);

        // Act
        var resultado = await repository.GetById(concesionario.Id);

        // Assert
        if (existe) resultado.Should().BeEquivalentTo(concesionario);
        else resultado.Should().BeNull();
        db.ChangeTracker.Entries().Should().BeEmpty();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetAll_ColeccionVaciaOConDatos_OrdenaPorNombre(bool conDatos) {
        // Arrange
        if (conDatos) {
            await using var preparacion = CrearContexto();
            preparacion.Concesionarios.AddRange(CrearConcesionario("Zeta"), CrearConcesionario("Alfa"));
            await preparacion.SaveChangesAsync();
        }
        await using var db = CrearContexto();
        var repository = new ConcesionarioRepository(db, NullLogger<ConcesionarioRepository>.Instance);

        // Act
        var resultado = await repository.GetAll();

        // Assert
        resultado.Select(c => c.Nombre).Should().Equal(conDatos ? new[] { "Alfa", "Zeta" } : Array.Empty<string>());
        db.ChangeTracker.Entries().Should().BeEmpty();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task SoftDelete_ExisteONo_ConservaHistorico(bool existe) {
        // Arrange
        var original = existe ? await GuardarConcesionario() : CrearConcesionario();
        await using var db = CrearContexto();
        var repository = new ConcesionarioRepository(db, NullLogger<ConcesionarioRepository>.Instance);

        // Act
        var resultado = await repository.SoftDelete(original.Id);

        // Assert
        resultado.Should().Be(existe);
        await using var lectura = CrearContexto();
        var guardado = await lectura.Concesionarios.SingleOrDefaultAsync();
        if (existe) {
            guardado!.IsDeleted.Should().BeTrue();
            guardado.UpdatedAt.Should().NotBeNull();
            guardado.Nombre.Should().Be(original.Nombre);
            guardado.CreatedAt.Should().Be(Fecha);
        } else guardado.Should().BeNull();
    }

    [Test]
    public async Task Update_ConcesionarioActivo_ActualizaDatosYConservaIdYCreacion() {
        // Arrange
        var original = await GuardarConcesionario();
        var cambios = original with {
            Id = 999, Nombre = "Nuevo", Ciudad = "Toledo", Telefono = "925000000",
            Email = "nuevo@example.com", CreatedAt = Fecha.AddYears(1)
        };
        await using var db = CrearContexto();
        var repository = new ConcesionarioRepository(db, NullLogger<ConcesionarioRepository>.Instance);

        // Act
        var resultado = await repository.Update(original.Id, cambios);

        // Assert
        resultado.Should().NotBeNull();
        await using var lectura = CrearContexto();
        var guardado = await lectura.Concesionarios.SingleAsync();
        // PostgreSQL conserva microsegundos; DateTime puede contener décimas de microsegundo.
        guardado.Should().BeEquivalentTo(resultado, options => options.Excluding(c => c.UpdatedAt));
        guardado.UpdatedAt.Should().BeCloseTo(resultado!.UpdatedAt!.Value, TimeSpan.FromTicks(10));
        guardado.Id.Should().Be(original.Id);
        guardado.CreatedAt.Should().Be(Fecha);
        guardado.Nombre.Should().Be("Nuevo");
        guardado.Ciudad.Should().Be("Toledo");
        guardado.Telefono.Should().Be("925000000");
        guardado.Email.Should().Be("nuevo@example.com");
        guardado.UpdatedAt.Should().NotBeNull();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Update_EliminadoOInexistente_NoModificaDatos(bool existe) {
        // Arrange
        var original = existe ? await GuardarConcesionario(true) : CrearConcesionario();
        await using var db = CrearContexto();
        var repository = new ConcesionarioRepository(db, NullLogger<ConcesionarioRepository>.Instance);

        // Act
        var resultado = await repository.Update(original.Id, original with { Nombre = "No guardar" });

        // Assert
        resultado.Should().BeNull();
        await using var lectura = CrearContexto();
        var guardado = await lectura.Concesionarios.SingleOrDefaultAsync();
        if (existe) guardado.Should().BeEquivalentTo(original);
        else guardado.Should().BeNull();
    }

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, false)]
    public async Task ExistById_ActivoEliminadoOInexistente_IncluyeHistorico(bool existe, bool eliminado) {
        // Arrange
        var original = existe ? await GuardarConcesionario(eliminado) : CrearConcesionario();
        await using var db = CrearContexto();
        var repository = new ConcesionarioRepository(db, NullLogger<ConcesionarioRepository>.Instance);

        // Act
        var resultado = await repository.ExistById(original.Id);

        // Assert
        resultado.Should().Be(existe);
    }
}
