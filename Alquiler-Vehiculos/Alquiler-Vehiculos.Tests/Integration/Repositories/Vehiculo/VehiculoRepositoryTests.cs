using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Alquiler_Vehiculos.Tests.Integration.Repositories;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public partial class VehiculoRepositoryTests : RepositoryTestBase {
    [Test]
    public async Task Create_MatriculaDeVehiculoDeBaja_ContinuaReservada()
    {
        // Arrange
        var baja = await GuardarVehiculo(activo: false);
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);
        var repetido = CrearVehiculo(CrearFoto("otra.jpg"), baja.ConesionarioFk);

        // Act
        var existe = await repository.ExistByMatricula(baja.Matrícula);
        var existeId = await repository.ExistById(baja.Id);
        var resultado = await repository.Create(repetido);

        // Assert
        existe.Should().BeTrue();
        existeId.Should().BeTrue();
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(Alquiler_Vehiculos.Errors.Vehiculo.VehiculoError.VehiculoYaExiste(baja.Matrícula));
        await using var lectura = CrearContexto();
        (await lectura.Vehiculos.SingleAsync()).Activo.Should().BeFalse();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Listar_ConVehiculosDeBaja_SoloDevuelveYCuentaActivos(bool conActivo)
    {
        // Arrange
        await GuardarVehiculo("1234BCD", activo: false);
        if (conActivo) await GuardarVehiculo("5678FGH");
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var todos = (await repository.GetAll()).ToList();
        var pagina = await repository.GetPaged(1, 10);

        // Assert
        todos.Should().HaveCount(conActivo ? 1 : 0).And.OnlyContain(v => v.Activo);
        pagina.TotalItems.Should().Be(conActivo ? 1 : 0);
        pagina.Items.Select(v => v.Id).Should().BeEquivalentTo(todos.Select(v => v.Id));
        await using var lectura = CrearContexto();
        (await lectura.Vehiculos.CountAsync(v => !v.Activo)).Should().Be(1);
    }

    [Test]
    public async Task Create_MatriculaDuplicadaTrasConsultaEnOtroContexto_DevuelveConflicto()
    {
        // Arrange
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);
        (await repository.ExistByMatricula("1234BCD")).Should().BeFalse();
        var original = await GuardarVehiculo();
        var repetido = CrearVehiculo(CrearFoto("otra.jpg"), original.ConesionarioFk);

        // Act
        var resultado = await repository.Create(repetido);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(Alquiler_Vehiculos.Errors.Vehiculo.VehiculoError.VehiculoYaExiste("1234BCD"));
        await using var lectura = CrearContexto();
        (await lectura.Vehiculos.CountAsync()).Should().Be(1);
        (await lectura.Fotos.CountAsync()).Should().Be(1);
    }

    [Test]
    public async Task Create_ConcesionarioInexistente_NoConfundeFkConMatriculaDuplicada()
    {
        // Arrange
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);
        var vehiculo = CrearVehiculo(CrearFoto(), long.MaxValue);

        // Act
        Func<Task> guardar = () => repository.Create(vehiculo);

        // Assert
        var error = await guardar.Should().ThrowAsync<DbUpdateException>();
        error.Which.InnerException.Should().BeOfType<Npgsql.PostgresException>()
            .Which.SqlState.Should().Be(Npgsql.PostgresErrorCodes.ForeignKeyViolation);
    }

    [Test]
    public async Task Create_VehiculoValido_GeneraIdYPersisteRelacionFoto() {
        // Arrange
        var concesionario = await GuardarConcesionario();
        var vehiculo = CrearVehiculo(CrearFoto(), concesionario.Id);
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var resultado = await repository.Create(vehiculo);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().MatchRegex("^[A-Za-z0-9_-]{11}$");
        await using var lectura = CrearContexto();
        var guardado = await lectura.Vehiculos.Include(v => v.Foto).SingleAsync();
        guardado.Should().BeEquivalentTo(resultado.Value);
    }

    [TestCase(true, false)]
    [TestCase(true, true)]
    [TestCase(false, false)]
    public async Task Consultar_PorIdOMatricula_CargaFotoOSiNoExisteDevuelveNull(bool existe, bool porMatricula) {
        // Arrange
        var original = existe ? await GuardarVehiculo() : null;
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var resultado = porMatricula
            ? await repository.GetByMatricula(original?.Matrícula ?? "9999ZZZ")
            : await repository.GetById(original?.Id ?? "inexistente");

        // Assert
        if (existe) resultado.Should().BeEquivalentTo(original);
        else resultado.Should().BeNull();
        db.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Test]
    public async Task GetByMatricula_MatriculaInexistente_DevuelveNull() {
        // Arrange
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var resultado = await repository.GetByMatricula("9999ZZZ");

        // Assert
        resultado.Should().BeNull();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetAll_ColeccionVaciaOConDatos_OrdenaPorMatriculaECargaFotos(bool conDatos) {
        // Arrange
        if (conDatos) {
            await GuardarVehiculo("5678FGH");
            await GuardarVehiculo("1234BCD");
        }
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var resultado = (await repository.GetAll()).ToList();

        // Assert
        resultado.Select(v => v.Matrícula).Should().Equal(conDatos ? new[] { "1234BCD", "5678FGH" } : Array.Empty<string>());
        resultado.Should().OnlyContain(v => v.Foto != null && v.Foto.Nombre == v.FotoFk);
        db.ChangeTracker.Entries().Should().BeEmpty();
    }

    [TestCase(1, 1, "1234BCD")]
    [TestCase(2, 1, "5678FGH")]
    [TestCase(3, 1, "")]
    public async Task GetPaged_PaginaSolicitada_OrdenaCuentaYCargaFoto(int pagina, int tamano, string matricula) {
        // Arrange
        await GuardarVehiculo("5678FGH");
        await GuardarVehiculo("1234BCD");
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var resultado = await repository.GetPaged(pagina, tamano);

        // Assert
        resultado.TotalItems.Should().Be(2);
        resultado.Items.Select(v => v.Matrícula).Should().Equal(
            matricula == "" ? Array.Empty<string>() : new[] { matricula });
        resultado.Items.Should().OnlyContain(v => v.Foto != null && v.Foto.Nombre == v.FotoFk);
        db.ChangeTracker.Entries().Should().BeEmpty();
    }

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, false)]
    public async Task ExistById_YMatricula_ActivosEliminadosOInexistentes_IncluyenHistorico(bool existe, bool activo) {
        // Arrange
        var original = existe ? await GuardarVehiculo(activo: activo) : null;
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var porId = await repository.ExistById(original?.Id ?? "inexistente");
        var porMatricula = await repository.ExistByMatricula(original?.Matrícula ?? "9999ZZZ");

        // Assert
        porId.Should().Be(existe);
        porMatricula.Should().Be(existe);
    }

    [Test]
    public async Task SoftDelete_VehiculoDisponible_DejaRetiradoEInactivoYConservaHistorico() {
        // Arrange
        var original = await GuardarVehiculo();
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var resultado = await repository.SoftDelete(original.Id);

        // Assert
        resultado.Should().BeTrue();
        await using var lectura = CrearContexto();
        var guardado = await lectura.Vehiculos.SingleAsync();
        // RF-01 exige que ambos campos cambien conjuntamente; no se acepta conservar Activo=true.
        guardado.Should().BeEquivalentTo(new {
            Activo = false, Estado = EstadoVehiculo.Retirado,
            original.Id, original.Matrícula, original.CreatedAt
        });
        guardado.UpdatedAt.Should().NotBeNull();
        (await lectura.Fotos.CountAsync()).Should().Be(1);
    }

    [Test]
    public async Task SoftDelete_VehiculoInexistente_DevuelveFalse() {
        // Arrange
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var resultado = await repository.SoftDelete("inexistente");

        // Assert
        resultado.Should().BeFalse();
    }

    [Test]
    public async Task Update_VehiculoActivo_ActualizaDatosYConservaIdYCreacion() {
        // Arrange
        var original = await GuardarVehiculo();
        var cambios = original with {
            Id = "otro-id", Matrícula = "5678FGH", Marca = "Renault", Modelo = "Megane",
            PrecioBaseDia = 70m, Color = TipoColor.Rojo, Kilometraje = 15000,
            CreatedAt = Fecha.AddYears(1)
        };
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var resultado = await repository.Update(original.Id, cambios);

        // Assert
        resultado.Should().NotBeNull("un vehículo activo debe poder modificarse");
        await using var lectura = CrearContexto();
        var guardado = await lectura.Vehiculos.SingleAsync();
        guardado.Id.Should().Be(original.Id);
        guardado.CreatedAt.Should().Be(Fecha);
        guardado.Matrícula.Should().Be("5678FGH");
        guardado.Marca.Should().Be("Renault");
        guardado.Modelo.Should().Be("Megane");
        guardado.PrecioBaseDia.Should().Be(70m);
        guardado.Color.Should().Be(TipoColor.Rojo);
        guardado.Kilometraje.Should().Be(15000);
        guardado.UpdatedAt.Should().NotBeNull();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Update_VehiculoEliminadoOInexistente_NoModificaDatos(bool existe) {
        // Arrange
        var original = existe ? await GuardarVehiculo(activo: false) : null;
        var cambios = original is null ? CrearVehiculo(CrearFoto(), 999) : original with { Modelo = "No guardar" };
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var resultado = await repository.Update(original?.Id ?? "inexistente", cambios);

        // Assert
        resultado.Should().BeNull();
        await using var lectura = CrearContexto();
        var guardado = await lectura.Vehiculos.Include(v => v.Foto).SingleOrDefaultAsync();
        if (existe) guardado.Should().BeEquivalentTo(original);
        else guardado.Should().BeNull();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Restore_VehiculoEliminadoOInexistente_ReactivaSoloSiExiste(bool existe) {
        // Arrange
        var original = existe ? await GuardarVehiculo(activo: false) : null;
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var resultado = await repository.Restore(original?.Id ?? "inexistente");

        // Assert
        await using var lectura = CrearContexto();
        var guardado = await lectura.Vehiculos.SingleOrDefaultAsync();
        if (existe) {
            resultado.Should().NotBeNull();
            guardado!.Activo.Should().BeTrue();
            guardado.DeletedAt.Should().BeNull();
            guardado.UpdatedAt.Should().NotBeNull();
            guardado.Id.Should().Be(original!.Id);
            guardado.CreatedAt.Should().Be(Fecha);
        } else {
            resultado.Should().BeNull();
            guardado.Should().BeNull();
        }
    }
}
