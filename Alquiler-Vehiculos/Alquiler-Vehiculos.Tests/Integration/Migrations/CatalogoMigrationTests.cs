using Alquiler_Vehiculos.Data.Vehiculo;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Testcontainers.PostgreSql;

namespace Alquiler_Vehiculos.Tests.Integration.Migrations;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class CatalogoMigrationTests {
    private PostgreSqlContainer? _container;
    private DbContextOptions<PostgreDbContext> _options = null!;

    [OneTimeSetUp]
    public async Task ArrancarPostgreSql() {
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("migration_tests").Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await _container.StartAsync(timeout.Token);
        _options = new DbContextOptionsBuilder<PostgreDbContext>()
            .UseNpgsql(_container.GetConnectionString()).Options;
    }

    [OneTimeTearDown]
    public async Task EliminarPostgreSql() {
        if (_container is not null) await _container.DisposeAsync();
    }

    [SetUp]
    public async Task LimpiarEsquema() {
        // Cada caso parte de una base sin esquema para comprobar la migración de verdad.
        await using var db = new PostgreDbContext(_options);
        await db.Database.ExecuteSqlRawAsync("DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
    }

    private static async Task<int> ContarTablasDelCatalogo(PostgreDbContext db) =>
        await db.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*)::int AS \"Value\" FROM information_schema.tables " +
                "WHERE table_schema = 'public' AND table_name IN ('Concesionarios', 'Fotos', 'Vehiculos')")
            .SingleAsync();

    [Test]
    public async Task MigrateAsync_BaseVacia_CreaTablasYRegistraLaMigracion() {
        // Arrange
        await using var db = new PostgreDbContext(_options);

        // Act
        await db.Database.MigrateAsync();

        // Assert
        (await ContarTablasDelCatalogo(db)).Should().Be(3);
        var historial = await db.Database.SqlQueryRaw<string>(
                "SELECT \"MigrationId\" AS \"Value\" FROM \"__EFMigrationsHistory\"")
            .ToArrayAsync();
        historial.Should().Contain("20261123191344_InitialCreate");
        (await db.Vehiculos.CountAsync()).Should().Be(0);
        (await db.Fotos.CountAsync()).Should().Be(0);
        (await db.Concesionarios.CountAsync()).Should().Be(0);
    }

    [Test]
    public async Task MigrateAsync_MigracionCero_EliminaLasTablasDelCatalogo() {
        // Arrange
        await using var db = new PostgreDbContext(_options);
        await db.Database.MigrateAsync();
        var migrador = db.GetService<IMigrator>();

        // Act
        await migrador.MigrateAsync("0");

        // Assert
        (await ContarTablasDelCatalogo(db)).Should().Be(0);
        var historial = await db.Database.SqlQueryRaw<string>(
                "SELECT \"MigrationId\" AS \"Value\" FROM \"__EFMigrationsHistory\"")
            .ToArrayAsync();
        historial.Should().BeEmpty();
    }
}
