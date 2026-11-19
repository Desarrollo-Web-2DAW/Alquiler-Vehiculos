using Alquiler_Vehiculos.Services.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace Alquiler_Vehiculos.Storage.Tests;

public abstract class StorageTestBase {
    protected string RootPath = null!;

    [SetUp]
    public void PrepararDirectorioDelContenedor() {
        // Impide ejecutar accidentalmente estas operaciones de disco fuera de Docker.
        if (Environment.GetEnvironmentVariable("STORAGE_TEST_CONTAINER") != "1")
            throw new InvalidOperationException("Ejecuta StorageContainerTests desde Alquiler-Vehiculos.Tests; estos tests requieren Docker.");
        RootPath = Path.Combine("/tmp", "alquiler-storage-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
    }

    [TearDown]
    public void LimpiarDirectorioDelContenedor() {
        if (RootPath is not null && Directory.Exists(RootPath)) Directory.Delete(RootPath, true);
    }

    protected static IConfiguration Configuracion(Dictionary<string, string?>? valores = null) =>
        new ConfigurationBuilder().AddInMemoryCollection(valores ?? new Dictionary<string, string?>()).Build();

    protected StorageService CrearServicio(IConfiguration? configuracion = null, string? webRoot = null) =>
        new(configuracion ?? Configuracion(), NullLogger<StorageService>.Instance,
            new EntornoStorage { ContentRootPath = RootPath, WebRootPath = webRoot! });

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
