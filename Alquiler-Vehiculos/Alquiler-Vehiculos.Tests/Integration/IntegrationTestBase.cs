using Alquiler_Vehiculos.Entities;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Alquiler_Vehiculos.Tests.Integration;

/// <summary>
/// Base para las pruebas de integración con PostgreSQL real mediante Testcontainers.
/// Cada fixture levanta su propio contenedor y cada prueba empieza con la tabla de clientes vacía.
/// </summary>
[Category("Integration")]
public abstract class IntegrationTestBase {
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("test_db")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private DbContextOptions<AppDbContextUsuarios> _opciones = null!;

    /// <summary>Contexto de la prueba, sobre la base de datos del contenedor.</summary>
    protected AppDbContextUsuarios Db { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task ClassSetUp() {
        await _postgres.StartAsync();
    }

    [OneTimeTearDown]
    public async Task ClassTearDown() {
        await _postgres.DisposeAsync();
    }

    [SetUp]
    public async Task SetUp() {
        _opciones = new DbContextOptionsBuilder<AppDbContextUsuarios>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        Db = new AppDbContextUsuarios(_opciones);
        await Db.Database.EnsureCreatedAsync();

        // Vaciar la tabla antes de cada prueba para que no se mezclen datos.
        await using var connection = new Npgsql.NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            TRUNCATE TABLE "clientes" RESTART IDENTITY CASCADE;
            """;
        await command.ExecuteNonQueryAsync();
    }

    [TearDown]
    public async Task TearDown() {
        await Db.DisposeAsync();
    }

    /// <summary>
    /// Abre un contexto nuevo sobre la misma base de datos, para comprobar lo que
    /// de verdad se guardó y no lo que el contexto de la prueba tiene en memoria.
    /// </summary>
    /// <returns>Un contexto nuevo; quien lo pide debe liberarlo.</returns>
    protected AppDbContextUsuarios NuevoContexto() {
        return new AppDbContextUsuarios(_opciones);
    }
}