using Alquiler_Vehiculos.Data.Vehiculo;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Models.Vehiculo;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;

namespace Alquiler_Vehiculos.Tests.Integration.Infrastructure;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class CatalogoInitializationExtensionsTests {
    private static readonly string[] Matriculas = [
        "1234BCD", "2345CDF", "3456DFG", "4567FGH", "5678GHJ", "6789HJK",
        "7890JKL", "8901KLM", "9012MNP", "0123NPR", "1357PRS", "2468RST"
    ];

    private PostgreSqlContainer? _postgres;
    private MongoDbContainer? _mongo;
    private MongoDbContext _marcas = null!;
    private DbContextOptions<PostgreDbContext> _options = null!;
    private string _raiz = null!;

    [OneTimeSetUp]
    public async Task ArrancarDependencias() {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        _postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("catalogo_init_tests").Build();
        await _postgres.StartAsync(timeout.Token);
        _mongo = new MongoDbBuilder("mongo:7.0").Build();
        await _mongo.StartAsync(timeout.Token);
        _options = new DbContextOptionsBuilder<PostgreDbContext>()
            .UseNpgsql(_postgres.GetConnectionString()).Options;
        _marcas = new MongoDbContext(new MongoClient(_mongo.GetConnectionString()).GetDatabase("catalogo_init_tests"));
    }

    [OneTimeTearDown]
    public async Task LiberarDependencias() {
        if (_mongo is not null) await _mongo.DisposeAsync();
        if (_postgres is not null) await _postgres.DisposeAsync();
    }

    [SetUp]
    public async Task PrepararEscenarioLimpio() {
        _raiz = Path.Combine(Path.GetTempPath(), "catalogo-init-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_raiz, "uploads", "vehiculos"));
        foreach (var matricula in Matriculas) {
            await File.WriteAllBytesAsync(Path.Combine(_raiz, "uploads", "vehiculos", $"seed-{matricula}.jpg"), [1, 2, 3]);
        }

        await using var db = new PostgreDbContext(_options);
        await db.Database.ExecuteSqlRawAsync("DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
        await _marcas.Marcas.DeleteManyAsync(FilterDefinition<MarcaModel>.Empty);
    }

    [TearDown]
    public void LimpiarEscenario() {
        if (Directory.Exists(_raiz)) Directory.Delete(_raiz, true);
    }

    private WebApplication CrearAplicacion() {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            ContentRootPath = _raiz, WebRootPath = _raiz, EnvironmentName = "Testing"
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> {
            ["Storage:UploadPath"] = "uploads",
            ["Storage:Folders:Vehiculos"] = "vehiculos"
        });
        var cliente = new MongoClient(_mongo!.GetConnectionString());
        builder.Services.AddDbContext<PostgreDbContext>(opciones => opciones.UseNpgsql(_postgres!.GetConnectionString()));
        builder.Services.AddSingleton(new MongoDbContext(cliente.GetDatabase("catalogo_init_tests")));
        return builder.Build();
    }

    [Test]
    public async Task InitializeCatalogoAsync_SinDesarrollo_SoloAplicaMigraciones() {
        // Arrange
        await using var app = CrearAplicacion();

        // Act
        await app.InitializeCatalogoAsync(false);

        // Assert
        await using var lectura = new PostgreDbContext(_options);
        (await lectura.Fotos.CountAsync()).Should().Be(0);
        (await lectura.Concesionarios.CountAsync()).Should().Be(0);
        (await lectura.Vehiculos.CountAsync()).Should().Be(0);
        (await _marcas.Marcas.CountDocumentsAsync(FilterDefinition<MarcaModel>.Empty)).Should().Be(0);
    }

    [Test]
    public async Task InitializeCatalogoAsync_EnDesarrollo_AplicaMigracionesYSiembraElCatalogo() {
        // Arrange
        await using var app = CrearAplicacion();

        // Act
        await app.InitializeCatalogoAsync(true);

        // Assert
        await using var lectura = new PostgreDbContext(_options);
        (await lectura.Fotos.CountAsync()).Should().Be(12);
        (await lectura.Concesionarios.CountAsync()).Should().Be(5);
        var vehiculos = await lectura.Vehiculos.Include(v => v.Foto).ToArrayAsync();
        vehiculos.Should().HaveCount(12);
        vehiculos.Should().OnlyContain(v => v.FotoFk == v.Foto.Nombre);
        (await _marcas.Marcas.CountDocumentsAsync(FilterDefinition<MarcaModel>.Empty)).Should().Be(8);
    }
}
