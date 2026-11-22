using Alquiler_Vehiculos.Data.Seed.Storage;
using Alquiler_Vehiculos.Data.Seed.Vehiculo;
using Alquiler_Vehiculos.Data.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Tests.Integration.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Alquiler_Vehiculos.Tests.Integration.Data.Seed;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class CatalogoSeederTests : RepositoryTestBase {
    private MongoDbContainer? _mongo;
    private MongoClient? _client;
    private MongoDbContext _marcas = null!;
    private string _directorio = null!;
    private PhysicalFileProvider _archivos = null!;

    private sealed class Entorno : IWebHostEnvironment {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "Alquiler-Vehiculos.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public string WebRootPath { get; set; } = null!;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }

    [OneTimeSetUp]
    public async Task PrepararMongoYArchivos() {
        _mongo = new MongoDbBuilder("mongo:7.0").Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await _mongo.StartAsync(timeout.Token);
        _client = new MongoClient(_mongo.GetConnectionString());
        _marcas = new MongoDbContext(_client.GetDatabase("seed_tests"));
        _directorio = Path.Combine(Path.GetTempPath(), "alquiler-seed-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(_directorio, "uploads", "vehiculos"));
        foreach (var vehiculo in VehiculoSeeder.BuildVehiculos(Fecha)) {
            await File.WriteAllBytesAsync(Path.Combine(_directorio, "uploads", "vehiculos", $"seed-{vehiculo.Matrícula}.jpg"), [1, 2, 3]);
        }
        _archivos = new PhysicalFileProvider(_directorio);
    }

    [SetUp]
    public async Task LimpiarMarcas() => await _marcas.Marcas.DeleteManyAsync(FilterDefinition<MarcaModel>.Empty);

    [OneTimeTearDown]
    public async Task LiberarMongoYArchivos() {
        try {
            _archivos?.Dispose();
            _client?.Dispose();
            if (_directorio is not null && Directory.Exists(_directorio)) Directory.Delete(_directorio, true);
        } finally {
            if (_mongo is not null) await _mongo.DisposeAsync();
        }
    }

    private Entorno CrearEntorno(bool webRoot) => new() {
        ContentRootPath = _directorio, ContentRootFileProvider = _archivos,
        WebRootPath = webRoot ? _directorio : null!, WebRootFileProvider = _archivos
    };

    [Test]
    public async Task ConcesionarioSeedAsync_DosEjecuciones_PersisteSinDuplicarNiModificar() {
        // Arrange
        await using var db = CrearContexto();
        await ConcesionarioSeeder.SeedAsync(db);
        var originales = await db.Concesionarios.AsNoTracking().OrderBy(c => c.Id).ToArrayAsync();

        // Act
        await ConcesionarioSeeder.SeedAsync(db);

        // Assert
        await using var lectura = CrearContexto();
        originales.Should().HaveCount(5);
        (await lectura.Concesionarios.OrderBy(c => c.Id).ToArrayAsync()).Should().BeEquivalentTo(originales);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task FotoSeedAsync_ArchivosPreparados_PersisteMetadatosSinDuplicar(bool webRoot) {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var entorno = CrearEntorno(webRoot);
        await using var db = CrearContexto();

        // Act
        await FotoSeeder.SeedAsync(db, configuration, entorno);
        await FotoSeeder.SeedAsync(db, configuration, entorno);

        // Assert
        await using var lectura = CrearContexto();
        var fotos = await lectura.Fotos.ToArrayAsync();
        fotos.Should().HaveCount(12).And.OnlyContain(f => f.Tamano == 3 && f.Activo && f.Extension == ".jpg");
        fotos.Select(f => f.Nombre).Should().OnlyHaveUniqueItems();
        fotos.Should().OnlyContain(f => f.Ruta == "/uploads/vehiculos/" + f.Nombre);
    }

    [Test]
    public async Task MarcaSeedAsync_DosEjecuciones_PersisteSinDuplicar() {
        // Arrange
        await MarcaSeeder.SeedAsync(_marcas);
        var originales = await _marcas.Marcas.Find(FilterDefinition<MarcaModel>.Empty).ToListAsync();

        // Act
        await MarcaSeeder.SeedAsync(_marcas);

        // Assert
        originales.Should().HaveCount(8);
        (await _marcas.Marcas.Find(FilterDefinition<MarcaModel>.Empty).ToListAsync()).Should().BeEquivalentTo(originales);
    }

    [Test]
    public async Task VehiculoSeedAsync_CatalogoPreparado_PersisteRelacionesYSinDuplicados() {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        await using var db = CrearContexto();
        await ConcesionarioSeeder.SeedAsync(db);
        await MarcaSeeder.SeedAsync(_marcas);
        await FotoSeeder.SeedAsync(db, configuration, CrearEntorno(false));

        // Act
        await VehiculoSeeder.SeedAsync(db, _marcas, configuration);
        await VehiculoSeeder.SeedAsync(db, _marcas, configuration);

        // Assert
        await using var lectura = CrearContexto();
        var vehiculos = await lectura.Vehiculos.Include(v => v.Foto).ToArrayAsync();
        vehiculos.Should().HaveCount(12);
        vehiculos.Select(v => v.Matrícula).Should().OnlyHaveUniqueItems();
        vehiculos.Should().OnlyContain(v => v.FotoFk == v.Foto.Nombre && v.Foto.Ruta.EndsWith("seed-" + v.Matrícula + ".jpg"));
        var ids = await lectura.Concesionarios.Select(c => c.Id).ToArrayAsync();
        vehiculos.Should().OnlyContain(v => ids.Contains(v.ConesionarioFk));
    }

    [Test]
    public async Task VehiculoSeedAsync_SinFotos_NoInsertaFlota() {
        // Arrange
        await using var db = CrearContexto();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        Func<Task> act = () => VehiculoSeeder.SeedAsync(db, _marcas, configuration);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Faltan fotos de la flota.*");
        await using var lectura = CrearContexto();
        (await lectura.Vehiculos.CountAsync()).Should().Be(0);
    }
}
