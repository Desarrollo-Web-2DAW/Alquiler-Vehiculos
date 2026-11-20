using Alquiler_Vehiculos.Data.Vehiculo;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using VehiculoModel = Alquiler_Vehiculos.Models.Vehiculo.Vehiculo;

namespace Alquiler_Vehiculos.Tests.Integration.Data.Vehiculo;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class PostgreDbContextTests {
    private PostgreSqlContainer? _container;
    private DbContextOptions<PostgreDbContext> _options = null!;
    private static readonly DateTime Fecha = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    [OneTimeSetUp]
    public async Task ArrancarContenedor() {
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("catalogo_tests")
            .Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await _container.StartAsync(timeout.Token);
        _options = new DbContextOptionsBuilder<PostgreDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        // Este contexto no tiene migraciones; las existentes son de usuarios.
        await using var db = CrearContexto();
        await db.Database.EnsureCreatedAsync();
    }

    [SetUp]
    public async Task LimpiarDatos() {
        await using var db = CrearContexto();
        await db.Vehiculos.ExecuteDeleteAsync();
        await db.Fotos.ExecuteDeleteAsync();
        await db.Concesionarios.ExecuteDeleteAsync();
    }

    [OneTimeTearDown]
    public async Task EliminarContenedor() {
        if (_container is not null) await _container.DisposeAsync();
    }

    private PostgreDbContext CrearContexto() => new(_options);

    private static Foto CrearFoto(string ruta = "https://example.com/uploads/vehiculos/coche.jpg") => new() {
        Ruta = ruta, Nombre = "coche.jpg", Extension = ".jpg", Tamano = 1024,
        Activo = true, CreatedAt = Fecha
    };

    private async Task<Concesionario> GuardarConcesionario() {
        await using var db = CrearContexto();
        var concesionario = new Concesionario(0, "Motor Norte", "Santander", "942000111",
            "contacto@example.com", false, Fecha, null, null);
        db.Concesionarios.Add(concesionario);
        await db.SaveChangesAsync();
        return concesionario;
    }

    private static VehiculoModel CrearVehiculo(Foto foto, long concesionario,
        string matricula = "1234BCD", decimal precio = 45.50m, bool incluirFoto = true) => new() {
        FotoFk = foto.Ruta, Foto = incluirFoto ? foto : null!, Matrícula = matricula,
        Marca = "Toyota", Modelo = "Corolla", Acriss = "ILAH",
        Carroceria = TipoCarroceria.Sedan, Combustible = TipoCombustible.Hibrido,
        Transmision = TipoTransmision.Automatica, Estado = EstadoVehiculo.Disponible,
        Color = TipoColor.Azul, PrecioBaseDia = precio, Plazas = 5,
        MaleteroLitros = 361, AutonomiaKm = 700, Kilometraje = 12000,
        FechaUltimaRevision = Fecha, ConesionarioFk = concesionario, Activo = true,
        CreatedAt = Fecha
    };

    private async Task<VehiculoModel> GuardarVehiculo() {
        var concesionario = await GuardarConcesionario();
        await using var db = CrearContexto();
        var vehiculo = CrearVehiculo(CrearFoto(), concesionario.Id);
        db.Vehiculos.Add(vehiculo);
        await db.SaveChangesAsync();
        return vehiculo;
    }

    [Test]
    public async Task SaveChanges_ConcesionarioSinId_GeneraIdYPersisteDatos() {
        // Arrange
        await using var db = CrearContexto();
        var concesionario = new Concesionario(0, "Motor Norte", "Santander", "942000111",
            "contacto@example.com", false, Fecha, null, null);
        db.Concesionarios.Add(concesionario);

        // Act
        await db.SaveChangesAsync();

        // Assert
        concesionario.Id.Should().BeGreaterThan(0);
        await using var lectura = CrearContexto();
        var guardado = await lectura.Concesionarios.SingleAsync(c => c.Id == concesionario.Id);
        guardado.Should().BeEquivalentTo(concesionario);
    }

    [Test]
    public async Task SaveChanges_FotoValida_PersisteTodosLosCampos() {
        // Arrange
        await using var db = CrearContexto();
        var foto = CrearFoto();
        db.Fotos.Add(foto);

        // Act
        await db.SaveChangesAsync();

        // Assert
        await using var lectura = CrearContexto();
        var guardada = await lectura.Fotos.SingleAsync(f => f.Ruta == foto.Ruta);
        guardada.Should().BeEquivalentTo(foto);
    }

    [Test]
    public async Task SaveChanges_VehiculoValido_GeneraIdYPersisteTodosLosCampos() {
        // Arrange
        var concesionario = await GuardarConcesionario();
        await using var db = CrearContexto();
        var vehiculo = CrearVehiculo(CrearFoto(), concesionario.Id);
        db.Vehiculos.Add(vehiculo);

        // Act
        await db.SaveChangesAsync();

        // Assert
        vehiculo.Id.Should().MatchRegex("^[A-Za-z0-9_-]{11}$");
        await using var lectura = CrearContexto();
        var guardado = await lectura.Vehiculos.Include(v => v.Foto).SingleAsync(v => v.Id == vehiculo.Id);
        guardado.Should().BeEquivalentTo(vehiculo);
        guardado.Should().NotBeSameAs(vehiculo);
    }

    [Test]
    public async Task SaveChanges_FotoFkConUrlExistente_CargaFotoPorSuRuta() {
        // Arrange
        var concesionario = await GuardarConcesionario();
        var foto = CrearFoto();
        await using var db = CrearContexto();
        db.Fotos.Add(foto);
        await db.SaveChangesAsync();
        var vehiculo = CrearVehiculo(foto, concesionario.Id, incluirFoto: false);
        db.Vehiculos.Add(vehiculo);

        // Act
        await db.SaveChangesAsync();

        // Assert
        await using var lectura = CrearContexto();
        var guardado = await lectura.Vehiculos.Include(v => v.Foto).SingleAsync(v => v.Id == vehiculo.Id);
        guardado.FotoFk.Should().Be(foto.Ruta);
        guardado.Foto.Should().BeEquivalentTo(foto);
    }

    [Test]
    public async Task SaveChanges_FotoFkConUrlInexistente_RechazaClaveForanea() {
        // Arrange
        var concesionario = await GuardarConcesionario();
        await using var db = CrearContexto();
        db.Vehiculos.Add(CrearVehiculo(CrearFoto(), concesionario.Id, incluirFoto: false));

        // Act
        Func<Task> guardar = () => db.SaveChangesAsync();

        // Assert
        var error = await guardar.Should().ThrowAsync<DbUpdateException>();
        error.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        await using var lectura = CrearContexto();
        (await lectura.Vehiculos.CountAsync()).Should().Be(0);
    }

    [Test]
    public async Task SaveChanges_DosVehiculosConLaMismaFoto_RechazaRelacionDuplicada() {
        // Arrange
        var existente = await GuardarVehiculo();
        await using var db = CrearContexto();
        var foto = await db.Fotos.SingleAsync(f => f.Ruta == existente.FotoFk);
        db.Vehiculos.Add(CrearVehiculo(foto, existente.ConesionarioFk, "5678FGH", incluirFoto: false));

        // Act
        Func<Task> guardar = () => db.SaveChangesAsync();

        // Assert
        var error = await guardar.Should().ThrowAsync<DbUpdateException>();
        error.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        await using var lectura = CrearContexto();
        (await lectura.Vehiculos.CountAsync()).Should().Be(1);
    }

    [Test]
    public async Task SaveChanges_DosVehiculosNuevos_GeneraIdsDistintos() {
        // Arrange
        var concesionario = await GuardarConcesionario();
        await using var db = CrearContexto();
        var primero = CrearVehiculo(CrearFoto("/uploads/uno.jpg"), concesionario.Id);
        var segundo = CrearVehiculo(CrearFoto("/uploads/dos.jpg"), concesionario.Id, "5678FGH");
        db.Vehiculos.AddRange(primero, segundo);

        // Act
        await db.SaveChangesAsync();

        // Assert
        primero.Id.Should().MatchRegex("^[A-Za-z0-9_-]{11}$");
        segundo.Id.Should().MatchRegex("^[A-Za-z0-9_-]{11}$").And.NotBe(primero.Id);
        await using var lectura = CrearContexto();
        (await lectura.Vehiculos.CountAsync()).Should().Be(2);
    }

    [Test]
    public async Task SingleOrDefault_VehiculoInexistente_DevuelveNull() {
        // Arrange
        await using var db = CrearContexto();

        // Act
        var resultado = await db.Vehiculos.SingleOrDefaultAsync(v => v.Id == "inexistente");

        // Assert
        resultado.Should().BeNull();
    }

    [TestCase(null, PostgresErrorCodes.NotNullViolation)]
    [TestCase("12345BCD", PostgresErrorCodes.StringDataRightTruncation)]
    public async Task SaveChanges_MatriculaNulaODemasiadoLarga_RechazaEscritura(
        string? matricula, string codigo) {
        // Arrange
        var concesionario = await GuardarConcesionario();
        await using var db = CrearContexto();
        db.Vehiculos.Add(CrearVehiculo(CrearFoto(), concesionario.Id, matricula!));

        // Act
        Func<Task> guardar = () => db.SaveChangesAsync();

        // Assert
        var error = await guardar.Should().ThrowAsync<DbUpdateException>();
        error.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.SqlState.Should().Be(codigo);
        await using var lectura = CrearContexto();
        (await lectura.Vehiculos.CountAsync()).Should().Be(0);
    }

    [Test]
    public async Task SaveChanges_ConcesionarioInexistente_RechazaClaveForanea() {
        // Arrange
        await using var db = CrearContexto();
        db.Vehiculos.Add(CrearVehiculo(CrearFoto(), long.MaxValue));

        // Act
        Func<Task> guardar = () => db.SaveChangesAsync();

        // Assert
        var error = await guardar.Should().ThrowAsync<DbUpdateException>();
        error.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        await using var lectura = CrearContexto();
        (await lectura.Vehiculos.CountAsync()).Should().Be(0);
    }

    [Test]
    public async Task SaveChanges_PrecioConTresDecimales_RespetaEscalaDeDosDecimales() {
        // Arrange
        var concesionario = await GuardarConcesionario();
        await using var db = CrearContexto();
        var vehiculo = CrearVehiculo(CrearFoto(), concesionario.Id, precio: 45.567m);
        db.Vehiculos.Add(vehiculo);

        // Act
        await db.SaveChangesAsync();

        // Assert
        await using var lectura = CrearContexto();
        var guardado = await lectura.Vehiculos.SingleAsync(v => v.Id == vehiculo.Id);
        guardado.PrecioBaseDia.Should().Be(45.57m);
    }

    [Test]
    public async Task ExecuteUpdate_VehiculoExistente_PersistePrecioYEstadoActivo() {
        // Arrange
        var vehiculo = await GuardarVehiculo();
        await using var db = CrearContexto();

        // Act
        var modificados = await db.Vehiculos.Where(v => v.Id == vehiculo.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.PrecioBaseDia, 60m)
                .SetProperty(v => v.Activo, false));

        // Assert
        modificados.Should().Be(1);
        await using var lectura = CrearContexto();
        var guardado = await lectura.Vehiculos.SingleAsync(v => v.Id == vehiculo.Id);
        guardado.PrecioBaseDia.Should().Be(60m);
        guardado.Activo.Should().BeFalse();
    }

    [TestCase("Foto")]
    [TestCase("Concesionario")]
    public async Task SaveChanges_BorrarEntidadReferenciada_ImpideBorrado(string entidad) {
        // Arrange
        var vehiculo = await GuardarVehiculo();
        await using var db = CrearContexto();
        if (entidad == "Foto") {
            db.Fotos.Remove(await db.Fotos.SingleAsync(f => f.Ruta == vehiculo.FotoFk));
        } else {
            db.Concesionarios.Remove(await db.Concesionarios.SingleAsync(c => c.Id == vehiculo.ConesionarioFk));
        }

        // Act
        Func<Task> borrar = () => db.SaveChangesAsync();

        // Assert
        var error = await borrar.Should().ThrowAsync<DbUpdateException>();
        error.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        await using var lectura = CrearContexto();
        (await lectura.Vehiculos.CountAsync()).Should().Be(1);
        (await lectura.Fotos.CountAsync()).Should().Be(1);
        (await lectura.Concesionarios.CountAsync()).Should().Be(1);
    }

    [Test]
    public async Task SaveChanges_BorrarVehiculo_ConservaFotoYConcesionario() {
        // Arrange
        var vehiculo = await GuardarVehiculo();
        await using var db = CrearContexto();
        db.Vehiculos.Remove(await db.Vehiculos.SingleAsync(v => v.Id == vehiculo.Id));

        // Act
        await db.SaveChangesAsync();

        // Assert
        await using var lectura = CrearContexto();
        (await lectura.Vehiculos.CountAsync()).Should().Be(0);
        (await lectura.Fotos.CountAsync()).Should().Be(1);
        (await lectura.Concesionarios.CountAsync()).Should().Be(1);
    }

    [Test]
    public void Modelo_Vehiculo_ConfiguraIndicesDeBusqueda() {
        // Arrange
        using var db = CrearContexto();

        // Act
        var indices = db.Model.FindEntityType(typeof(VehiculoModel))!.GetIndexes()
            .Select(i => string.Join(",", i.Properties.Select(p => p.Name))).ToArray();

        // Assert
        indices.Should().Contain(new[] { "Marca", "Modelo", "Carroceria", "Estado", "Combustible" });
    }

    [Test]
    public void Modelo_NavegacionFoto_UtilizaLaClaveForaneaFotoFk() {
        // Arrange
        using var db = CrearContexto();

        // Act
        var relacion = db.Model.FindEntityType(typeof(VehiculoModel))!
            .FindNavigation(nameof(VehiculoModel.Foto))!.ForeignKey;

        // Assert
        relacion.Properties.Select(p => p.Name).Should().Equal(nameof(VehiculoModel.FotoFk));
        relacion.IsUnique.Should().BeTrue();
        relacion.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }
}
