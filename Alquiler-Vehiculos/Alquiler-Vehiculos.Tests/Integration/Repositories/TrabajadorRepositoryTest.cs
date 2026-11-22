using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using Alquiler_Vehiculos.Repositories.Trabajadores;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace Alquiler_Vehiculos.Tests.Integration.Repositories;

[TestFixture]
[Category("Integration")]
public class TrabajadorRepositoryTests {
    private static readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:17-alpine").Build();
    private AppDbContextUsuarios CrearContexto() {
        var options = new DbContextOptionsBuilder<AppDbContextUsuarios>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        return new AppDbContextUsuarios(options);
    }
    private static Trabajador CrearTrabajador(
        string nombre = "Laura",
        string apellidos = "García Pérez",
        string email = "laura.garcia@empresa.es",
        string dni = "12345678Z",
        TipoTrabajador tipo = TipoTrabajador.Operaciones) => new() {
        Nombre = nombre,
        Apellidos = apellidos,
        Email = email,
        Telefono = "611222333",
        FechaNacimiento = new DateOnly(1995, 5, 10),
        Dni = dni,
        FechaIngreso = new DateOnly(2024, 3, 1),
        Tipo = tipo
    };
    [OneTimeSetUp]
    public async Task ArrancarPostgres() {
        await _postgres.StartAsync();
        await using var db = CrearContexto();
        await db.Database.MigrateAsync();
    }
    [OneTimeTearDown]
    public async Task PararPostgres() {
        await _postgres.DisposeAsync();
    }
    [SetUp]
    public async Task LimpiarTablas() {
        await using var db = CrearContexto();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE usuarios, trabajadores RESTART IDENTITY CASCADE;");
    }
    // ── Casos positivos ──────────────────────────────────────────────────
    [Test]
    public async Task Create_TrabajadorValido_PersisteEnPostgreSQL() {
        // Arrange
        await using var db = CrearContexto();
        var repo = new TrabajadorRepository(db, NullLogger<TrabajadorRepository>.Instance);
        var trabajador = CrearTrabajador();
        // Act
        var creado = await repo.Create(trabajador);
        // Assert (con DbContext nuevo para comprobar BD real)
        await using var dbVerificacion = CrearContexto();
        var enBd = await dbVerificacion.Trabajadores.FindAsync(creado.Id);
        enBd.Should().NotBeNull();
        enBd!.Email.Should().Be("laura.garcia@empresa.es");
        enBd.Dni.Should().Be("12345678Z");
        enBd.Activo.Should().BeTrue();
        enBd.IsDeleted.Should().BeFalse();
    }
    [Test]
    public async Task GetAll_ConActivosYDadosDeBaja_DevuelveSoloNoBorradosOrdenados() {
        // Arrange
        await using (var dbSeed = CrearContexto()) {
            var t1 = CrearTrabajador("Carlos", "Zúñiga", "carlos@empresa.es", "11111111H");
            var t2 = CrearTrabajador("Ana", "Alonso", "ana@empresa.es", "00000000T");
            var borrado = CrearTrabajador("Borrado", "Baja", "baja@empresa.es", "00000022E");
            borrado.Activo = false;
            borrado.IsDeleted = true;
            borrado.DeletedAt = DateTime.UtcNow;
            dbSeed.Trabajadores.AddRange(t1, t2, borrado);
            await dbSeed.SaveChangesAsync();
        }
        await using var db = CrearContexto();
        var repo = new TrabajadorRepository(db, NullLogger<TrabajadorRepository>.Instance);
        // Act
        var lista = await repo.GetAll();
        // Assert
        lista.Should().HaveCount(2);
        lista.Select(t => t.Apellidos).Should().ContainInOrder("Alonso", "Zúñiga");
    }
    [Test]
    public async Task GetById_TrabajadorExistente_LoDevuelve() {
        // Arrange
        var trabajador = CrearTrabajador();
        await using (var dbSeed = CrearContexto()) {
            dbSeed.Trabajadores.Add(trabajador);
            await dbSeed.SaveChangesAsync();
        }
        await using var db = CrearContexto();
        var repo = new TrabajadorRepository(db, NullLogger<TrabajadorRepository>.Instance);
        // Act
        var encontrado = await repo.GetById(trabajador.Id);
        // Assert
        encontrado.Should().NotBeNull();
        encontrado!.Id.Should().Be(trabajador.Id);
        encontrado.Dni.Should().Be("12345678Z");
    }
    [Test]
    public async Task Update_TrabajadorActivo_ActualizaDatosEnPostgreSQL() {
        // Arrange
        var trabajador = CrearTrabajador();
        await using (var dbSeed = CrearContexto()) {
            dbSeed.Trabajadores.Add(trabajador);
            await dbSeed.SaveChangesAsync();
        }
        var nuevosDatos = CrearTrabajador(
            nombre: "Laura Editada",
            apellidos: "García López",
            email: "laura.nueva@empresa.es",
            dni: "12345678Z",
            tipo: TipoTrabajador.Mecanico);
        await using var db = CrearContexto();
        var repo = new TrabajadorRepository(db, NullLogger<TrabajadorRepository>.Instance);
        // Act
        var actualizado = await repo.Update(trabajador.Id, nuevosDatos);
        // Assert
        actualizado.Should().NotBeNull();
        await using var dbVerificacion = CrearContexto();
        var enBd = await dbVerificacion.Trabajadores.FindAsync(trabajador.Id);
        enBd!.Nombre.Should().Be("Laura Editada");
        enBd.Email.Should().Be("laura.nueva@empresa.es");
        enBd.Tipo.Should().Be(TipoTrabajador.Mecanico);
    }
    [Test]
    public async Task Delete_TrabajadorConUsuario_HaceBajaLogicaEnTrabajadorYUser() {
        // Arrange
        var trabajador = CrearTrabajador();
        var user = new User {
            UserName = trabajador.Email,
            NormalizedUserName = trabajador.Email.ToUpperInvariant(),
            Email = trabajador.Email,
            NormalizedEmail = trabajador.Email.ToUpperInvariant(),
            TrabajadorId = trabajador.Id
        };
        await using (var dbSeed = CrearContexto()) {
            dbSeed.Trabajadores.Add(trabajador);
            dbSeed.Users.Add(user);
            await dbSeed.SaveChangesAsync();
        }
        await using var db = CrearContexto();
        var repo = new TrabajadorRepository(db, NullLogger<TrabajadorRepository>.Instance);
        // Act
        var eliminado = await repo.Delete(trabajador.Id);
        // Assert
        eliminado.Should().BeTrue();
        await using var dbVerificacion = CrearContexto();
        var trabajadorBd = await dbVerificacion.Trabajadores.FindAsync(trabajador.Id);
        var userBd = await dbVerificacion.Users.FindAsync(user.Id);
        trabajadorBd!.Activo.Should().BeFalse();
        trabajadorBd.IsDeleted.Should().BeTrue();
        trabajadorBd.DeletedAt.Should().NotBeNull();
        userBd!.IsDeleted.Should().BeTrue();
        userBd.DeletedAt.Should().NotBeNull();
    }
    [Test]
    public async Task Restore_TrabajadorDadoDeBaja_ReactivaTrabajadorYUser() {
        // Arrange
        var trabajador = CrearTrabajador();
        trabajador.Activo = false;
        trabajador.IsDeleted = true;
        trabajador.DeletedAt = DateTime.UtcNow;
        var user = new User {
            UserName = trabajador.Email,
            NormalizedUserName = trabajador.Email.ToUpperInvariant(),
            Email = trabajador.Email,
            NormalizedEmail = trabajador.Email.ToUpperInvariant(),
            TrabajadorId = trabajador.Id,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };
        await using (var dbSeed = CrearContexto()) {
            dbSeed.Trabajadores.Add(trabajador);
            dbSeed.Users.Add(user);
            await dbSeed.SaveChangesAsync();
        }
        await using var db = CrearContexto();
        var repo = new TrabajadorRepository(db, NullLogger<TrabajadorRepository>.Instance);
        // Act
        var restaurado = await repo.Restore(trabajador.Id);
        // Assert
        restaurado.Should().NotBeNull();
        await using var dbVerificacion = CrearContexto();
        var trabajadorBd = await dbVerificacion.Trabajadores.FindAsync(trabajador.Id);
        var userBd = await dbVerificacion.Users.FindAsync(user.Id);
        trabajadorBd!.Activo.Should().BeTrue();
        trabajadorBd.IsDeleted.Should().BeFalse();
        trabajadorBd.DeletedAt.Should().BeNull();
        userBd!.IsDeleted.Should().BeFalse();
        userBd.DeletedAt.Should().BeNull();
    }
    [Test]
    public async Task EmailEnUso_MismoEmailConExcluirIdDelPropioTrabajador_DevuelveFalse() {
        // Arrange
        var trabajador = CrearTrabajador();
        await using (var dbSeed = CrearContexto()) {
            dbSeed.Trabajadores.Add(trabajador);
            await dbSeed.SaveChangesAsync();
        }
        await using var db = CrearContexto();
        var repo = new TrabajadorRepository(db, NullLogger<TrabajadorRepository>.Instance);
        // Act
        var enUso = await repo.EmailEnUso(trabajador.Email, trabajador.Id);
        // Assert
        enUso.Should().BeFalse();
    }
    // ── Casos negativos / existencia ─────────────────────────────────────
    [Test]
    public async Task GetById_IdInexistente_DevuelveNull() {
        // Arrange
        await using var db = CrearContexto();
        var repo = new TrabajadorRepository(db, NullLogger<TrabajadorRepository>.Instance);
        // Act
        var encontrado = await repo.GetById(Guid.NewGuid());
        // Assert
        encontrado.Should().BeNull();
    }
    [Test]
    public async Task Update_TrabajadorInexistente_DevuelveNull() {
        // Arrange
        await using var db = CrearContexto();
        var repo = new TrabajadorRepository(db, NullLogger<TrabajadorRepository>.Instance);
        // Act
        var resultado = await repo.Update(Guid.NewGuid(), CrearTrabajador());
        // Assert
        resultado.Should().BeNull();
    }
    [Test]
    public async Task Delete_TrabajadorInexistente_DevuelveFalse() {
        // Arrange
        await using var db = CrearContexto();
        var repo = new TrabajadorRepository(db, NullLogger<TrabajadorRepository>.Instance);
        // Act
        var eliminado = await repo.Delete(Guid.NewGuid());
        // Assert
        eliminado.Should().BeFalse();
    }
    [Test]
    public async Task EmailEnUsoYDniEnUso_ExistenteEnOtroTrabajador_DevuelvenTrue() {
        // Arrange
        var existente = CrearTrabajador(email: "ana@empresa.es", dni: "12345678Z");
        await using (var dbSeed = CrearContexto()) {
            dbSeed.Trabajadores.Add(existente);
            await dbSeed.SaveChangesAsync();
        }
        await using var db = CrearContexto();
        var repo = new TrabajadorRepository(db, NullLogger<TrabajadorRepository>.Instance);
        // Act
        var emailEnUso = await repo.EmailEnUso("ana@empresa.es", null);
        var dniEnUsoOtro = await repo.DniEnUso("12345678Z", Guid.NewGuid());
        // Assert
        emailEnUso.Should().BeTrue();
        dniEnUsoOtro.Should().BeTrue();
    }
        [Test]
    public async Task GetByNombre_TrabajadorExistente_LoDevuelve() {
        // Arrange
        var trabajador = CrearTrabajador(nombre: "María", email: "maria@empresa.es", dni: "12345678Z");
        await using (var dbSeed = CrearContexto()) {
            dbSeed.Trabajadores.Add(trabajador);
            await dbSeed.SaveChangesAsync();
        }

        await using var db = CrearContexto();
        var repo = new TrabajadorRepository(db, NullLogger<TrabajadorRepository>.Instance);

        // Act
        var encontrado = await repo.GetByNombre("María");

        // Assert
        encontrado.Should().NotBeNull();
        encontrado!.Id.Should().Be(trabajador.Id);
        encontrado.Nombre.Should().Be("María");
    }

    [Test]
    public async Task GetByNombre_InexistenteODadoDeBaja_DevuelveNull() {
        // Arrange
        var borrado = CrearTrabajador(nombre: "Borrado", email: "borrado@empresa.es", dni: "00000000T");
        borrado.Activo = false;
        borrado.IsDeleted = true;
        borrado.DeletedAt = DateTime.UtcNow;

        await using (var dbSeed = CrearContexto()) {
            dbSeed.Trabajadores.Add(borrado);
            await dbSeed.SaveChangesAsync();
        }

        await using var db = CrearContexto();
        var repo = new TrabajadorRepository(db, NullLogger<TrabajadorRepository>.Instance);

        // Act
        var porNombreBorrado = await repo.GetByNombre("Borrado");
        var porNombreInexistente = await repo.GetByNombre("NoExiste");

        // Assert
        porNombreBorrado.Should().BeNull();
        porNombreInexistente.Should().BeNull();
    }

    [Test]
    public async Task GetByEmail_TrabajadorExistente_LoDevuelve() {
        // Arrange
        var trabajador = CrearTrabajador(email: "ana.lopez@empresa.es", dni: "11111111H");
        await using (var dbSeed = CrearContexto()) {
            dbSeed.Trabajadores.Add(trabajador);
            await dbSeed.SaveChangesAsync();
        }

        await using var db = CrearContexto();
        var repo = new TrabajadorRepository(db, NullLogger<TrabajadorRepository>.Instance);

        // Act
        var encontrado = await repo.GetByEmail("ana.lopez@empresa.es");

        // Assert
        encontrado.Should().NotBeNull();
        encontrado!.Id.Should().Be(trabajador.Id);
        encontrado.Email.Should().Be("ana.lopez@empresa.es");
    }

    [Test]
    public async Task GetByEmail_InexistenteODadoDeBaja_DevuelveNull() {
        // Arrange
        var borrado = CrearTrabajador(email: "baja@empresa.es", dni: "00000022E");
        borrado.Activo = false;
        borrado.IsDeleted = true;
        borrado.DeletedAt = DateTime.UtcNow;

        await using (var dbSeed = CrearContexto()) {
            dbSeed.Trabajadores.Add(borrado);
            await dbSeed.SaveChangesAsync();
        }

        await using var db = CrearContexto();
        var repo = new TrabajadorRepository(db, NullLogger<TrabajadorRepository>.Instance);

        // Act
        var porEmailBorrado = await repo.GetByEmail("baja@empresa.es");
        var porEmailInexistente = await repo.GetByEmail("noexiste@empresa.es");

        // Assert
        porEmailBorrado.Should().BeNull();
        porEmailInexistente.Should().BeNull();
    }
}