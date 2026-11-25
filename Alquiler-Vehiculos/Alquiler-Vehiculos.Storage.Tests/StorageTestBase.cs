using Alquiler_Vehiculos.Data.Vehiculo;
using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Services.Storage;
using Alquiler_Vehiculos.Validators.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Alquiler_Vehiculos.Storage.Tests;

public abstract class StorageTestBase {
    protected string RootPath = null!;
    protected PostgreDbContext Db = null!;

    [SetUp]
    public async Task PrepararDirectorioDelContenedor() {
        // Impide ejecutar accidentalmente estas operaciones de disco fuera de Docker.
        if (Environment.GetEnvironmentVariable("STORAGE_TEST_CONTAINER") != "1")
            throw new InvalidOperationException("Ejecuta StorageContainerTests desde Alquiler-Vehiculos.Tests; estos tests requieren Docker.");
        RootPath = Path.Combine("/tmp", "alquiler-storage-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
        var conexion = Environment.GetEnvironmentVariable("STORAGE_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Falta la conexión al PostgreSQL de pruebas.");
        Db = new PostgreDbContext(new DbContextOptionsBuilder<PostgreDbContext>()
            .UseNpgsql(conexion).Options);
        await Db.Database.MigrateAsync();
        await Db.Vehiculos.ExecuteDeleteAsync();
        await Db.Fotos.ExecuteDeleteAsync();
        await Db.Concesionarios.ExecuteDeleteAsync();
    }

    [TearDown]
    public async Task LimpiarDirectorioDelContenedor() {
        if (Db is not null) await Db.DisposeAsync();
        if (RootPath is not null && Directory.Exists(RootPath)) Directory.Delete(RootPath, true);
    }

    protected static IConfiguration Configuracion(Dictionary<string, string?>? valores = null) =>
        new ConfigurationBuilder().AddInMemoryCollection(valores ?? new Dictionary<string, string?>()).Build();

    protected StorageService CrearServicio(IConfiguration? configuracion = null) =>
        new(Options.Create(CrearStorageSettings(configuracion)), NullLogger<StorageService>.Instance);

    protected FotoService CrearFotoService(
        IConfiguration? configuracion = null,
        IFotoRepository? repository = null) =>
        new(
            new FileValidator(Options.Create(CrearStorageSettings(configuracion))),
            repository ?? new FotoRepository(Db, NullLogger<FotoRepository>.Instance),
            CrearServicio(configuracion));

    private StorageSettings CrearStorageSettings(IConfiguration? configuracion) {
        var settings = new StorageSettings {
            CustomRootPath = RootPath,
            UploadPath = "uploads",
            MaxFileSize = 5 * 1024 * 1024,
            AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif"],
            AllowedContentTypes = ["image/jpeg", "image/png", "image/gif"]
        };
        configuracion?.GetSection(StorageSettings.SectionName).Bind(settings);
        if (string.IsNullOrEmpty(settings.CustomRootPath)) settings.CustomRootPath = RootPath;
        if (string.IsNullOrEmpty(settings.UploadPath)) settings.UploadPath = "uploads";
        return settings;
    }

    protected PostgreDbContext CrearContextoDeLectura() =>
        new(new DbContextOptionsBuilder<PostgreDbContext>()
            .UseNpgsql(Db.Database.GetConnectionString()).Options);

    protected WebApplication CrearAplicacion(string? webRoot = null) =>
        WebApplication.CreateBuilder(new WebApplicationOptions {
            ContentRootPath = RootPath, WebRootPath = webRoot,
            EnvironmentName = "Testing", Args = Array.Empty<string>()
        }).Build();

    private sealed class EntornoStorage : IWebHostEnvironment {
        public string ApplicationName { get; set; } = "StorageTests";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = null!;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
