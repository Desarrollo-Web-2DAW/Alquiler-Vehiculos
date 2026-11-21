using Alquiler_Vehiculos.Repositories.Storage;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Alquiler_Vehiculos.Tests.Integration.Repositories;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class FotoRepositoryTests : RepositoryTestBase {
    [Test]
    public async Task Create_FotoValida_PersisteTodosLosCampos() {
        // Arrange
        await using var db = CrearContexto();
        var repository = new FotoRepository(db, NullLogger<FotoRepository>.Instance);
        var foto = CrearFoto();

        // Act
        var resultado = await repository.Create(foto);

        // Assert
        resultado.Should().BeSameAs(foto);
        await using var lectura = CrearContexto();
        (await lectura.Fotos.SingleAsync()).Should().BeEquivalentTo(foto);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetById_FotoExistenteOInexistente_DevuelveResultadoEsperado(bool existe) {
        // Arrange
        var foto = CrearFoto();
        if (existe) await GuardarFoto(foto);
        await using var db = CrearContexto();
        var repository = new FotoRepository(db, NullLogger<FotoRepository>.Instance);

        // Act
        var resultado = await repository.GetById(foto.Nombre);

        // Assert
        if (existe) resultado.Should().BeEquivalentTo(foto);
        else resultado.Should().BeNull();
        db.ChangeTracker.Entries().Should().BeEmpty();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetAll_ColeccionVaciaOConFotos_DevuelveTodasOrdenadas(bool conDatos) {
        // Arrange
        if (conDatos) {
            await GuardarFoto(CrearFoto("zeta.jpg"));
            await GuardarFoto(CrearFoto("alfa.jpg"));
        }
        await using var db = CrearContexto();
        var repository = new FotoRepository(db, NullLogger<FotoRepository>.Instance);

        // Act
        var resultado = await repository.GetAll();

        // Assert
        resultado.Select(f => f.Nombre).Should().Equal(conDatos ? new[] { "alfa.jpg", "zeta.jpg" } : Array.Empty<string>());
        db.ChangeTracker.Entries().Should().BeEmpty();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task SoftDelete_FotoExistenteOInexistente_ConservaDocumentoYActualizaAuditoria(bool existe) {
        // Arrange
        var foto = CrearFoto();
        if (existe) await GuardarFoto(foto);
        await using var db = CrearContexto();
        var repository = new FotoRepository(db, NullLogger<FotoRepository>.Instance);

        // Act
        var resultado = await repository.SoftDelete(foto.Nombre);

        // Assert
        resultado.Should().Be(existe);
        await using var lectura = CrearContexto();
        (await lectura.Fotos.CountAsync()).Should().Be(existe ? 1 : 0);
        if (existe) {
            var guardada = await lectura.Fotos.SingleAsync();
            guardada.Activo.Should().BeFalse();
            guardada.DeletedAt.Should().NotBeNull();
            guardada.UpdatedAt.Should().NotBeNull();
            guardada.Ruta.Should().Be(foto.Ruta);
            guardada.CreatedAt.Should().Be(Fecha);
        }
    }

    [Test]
    public async Task Update_FotoActiva_ActualizaMetadatosYConservaClaveYCreacion() {
        // Arrange
        var foto = CrearFoto();
        await GuardarFoto(foto);
        var cambios = foto with { Extension = ".png", Tamano = 2048 };
        await using var db = CrearContexto();
        var repository = new FotoRepository(db, NullLogger<FotoRepository>.Instance);

        // Act
        var resultado = await repository.Update(foto.Nombre, cambios);

        // Assert
        resultado.Should().NotBeNull();
        await using var lectura = CrearContexto();
        var guardada = await lectura.Fotos.SingleAsync();
        // PostgreSQL conserva microsegundos; DateTime puede contener décimas de microsegundo.
        guardada.Should().BeEquivalentTo(resultado, options => options.Excluding(f => f.UpdatedAt));
        guardada.UpdatedAt.Should().BeCloseTo(resultado!.UpdatedAt!.Value, TimeSpan.FromTicks(10));
        guardada.Extension.Should().Be(".png");
        guardada.Tamano.Should().Be(2048);
        guardada.Nombre.Should().Be(foto.Nombre);
        guardada.Ruta.Should().Be(foto.Ruta);
        guardada.CreatedAt.Should().Be(Fecha);
        guardada.UpdatedAt.Should().NotBeNull();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Update_FotoEliminadaOInexistente_NoModificaDatos(bool existe) {
        // Arrange
        var foto = CrearFoto(activa: false);
        if (existe) await GuardarFoto(foto);
        await using var db = CrearContexto();
        var repository = new FotoRepository(db, NullLogger<FotoRepository>.Instance);

        // Act
        var resultado = await repository.Update(foto.Nombre, foto with { Tamano = 9999 });

        // Assert
        resultado.Should().BeNull();
        await using var lectura = CrearContexto();
        var guardada = await lectura.Fotos.SingleOrDefaultAsync();
        if (existe) guardada.Should().BeEquivalentTo(foto);
        else guardada.Should().BeNull();
    }

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, false)]
    public async Task ExistById_FotoActivaEliminadaOInexistente_IncluyeElHistorico(bool existe, bool activa) {
        // Arrange
        var foto = CrearFoto(activa: activa);
        if (existe) await GuardarFoto(foto);
        await using var db = CrearContexto();
        var repository = new FotoRepository(db, NullLogger<FotoRepository>.Instance);

        // Act
        var resultado = await repository.ExistById(foto.Nombre);

        // Assert
        resultado.Should().Be(existe);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Restore_FotoEliminadaOInexistente_ReactivaSoloSiExiste(bool existe) {
        // Arrange
        var foto = CrearFoto(activa: false);
        if (existe) await GuardarFoto(foto);
        await using var db = CrearContexto();
        var repository = new FotoRepository(db, NullLogger<FotoRepository>.Instance);

        // Act
        var resultado = await repository.Restore(foto.Nombre);

        // Assert
        await using var lectura = CrearContexto();
        var guardada = await lectura.Fotos.SingleOrDefaultAsync();
        if (existe) {
            resultado.Should().NotBeNull();
            guardada!.Activo.Should().BeTrue();
            guardada.DeletedAt.Should().BeNull();
            guardada.UpdatedAt.Should().NotBeNull();
            guardada.CreatedAt.Should().Be(Fecha);
        } else {
            resultado.Should().BeNull();
            guardada.Should().BeNull();
        }
    }
}
