using Alquiler_Vehiculos.Data.Vehiculo;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Alquiler_Vehiculos.Tests.Integration.Repositories;

public abstract class RepositoryTestBase {
    private PostgreSqlContainer? _container;
    private DbContextOptions<PostgreDbContext> _options = null!;
    protected static readonly DateTime Fecha = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    [OneTimeSetUp]
    public async Task ArrancarPostgreSql() {
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("repository_tests").Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await _container.StartAsync(timeout.Token);
        _options = new DbContextOptionsBuilder<PostgreDbContext>()
            .UseNpgsql(_container.GetConnectionString()).Options;
        // El catálogo todavía no tiene migraciones; las existentes son de usuarios.
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
    public async Task EliminarPostgreSql() {
        if (_container is not null) await _container.DisposeAsync();
    }

    protected PostgreDbContext CrearContexto() => new(_options);

    protected static Foto CrearFoto(string nombre = "coche.jpg", bool activa = true) => new() {
        Nombre = nombre, Ruta = $"/uploads/vehiculos/{nombre}", Extension = ".jpg",
        Tamano = 1024, Activo = activa, CreatedAt = Fecha,
        DeletedAt = activa ? null : Fecha
    };

    protected static ConcesionarioModel CrearConcesionario(string nombre = "Motor Norte", bool eliminado = false) =>
        new(0, nombre, "Madrid", "910000111", "contacto@example.com", eliminado,
            Fecha, null, eliminado ? Fecha : null);

    protected async Task GuardarFoto(Foto foto) {
        await using var db = CrearContexto();
        db.Fotos.Add(foto);
        await db.SaveChangesAsync();
    }

    protected async Task<ConcesionarioModel> GuardarConcesionario(bool eliminado = false) {
        await using var db = CrearContexto();
        var concesionario = CrearConcesionario(eliminado: eliminado);
        db.Concesionarios.Add(concesionario);
        await db.SaveChangesAsync();
        return concesionario;
    }

    protected async Task<VehiculoModel> GuardarVehiculo(string matricula = "1234BCD", bool activo = true) {
        var concesionario = await GuardarConcesionario();
        await using var db = CrearContexto();
        var vehiculo = CrearVehiculo(CrearFoto($"{matricula}.jpg"), concesionario.Id) with {
            Matrícula = matricula, Activo = activo,
            DeletedAt = activo ? null : Fecha
        };
        vehiculo.CambiarEstadoMapper(activo ? EstadoVehiculo.Disponible : EstadoVehiculo.Retirado);
        db.Vehiculos.Add(vehiculo);
        await db.SaveChangesAsync();
        return vehiculo;
    }

    protected static VehiculoModel CrearVehiculo(Foto foto, long concesionario) => new() {
        FotoFk = foto.Nombre, Foto = foto, Matrícula = "1234BCD", Marca = "Toyota", Modelo = "Corolla",
        Acriss = "ILAH", Carroceria = TipoCarroceria.Sedan, Combustible = TipoCombustible.Hibrido,
        Transmision = TipoTransmision.Automatica,
        Color = TipoColor.Azul, PrecioBaseDia = 45.50m, Plazas = 5, MaleteroLitros = 361,
        AutonomiaKm = 700, Kilometraje = 12000, FechaUltimaRevision = Fecha,
        ConesionarioFk = concesionario, Activo = true, CreatedAt = Fecha
    };
}
