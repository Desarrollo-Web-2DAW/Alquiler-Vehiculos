using System.Xml.Linq;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Alquiler_Vehiculos.Tests.Integration.Storage;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class StorageContainerTests {
    private IContainer? _container;
    private PostgreSqlContainer? _postgres;

    [OneTimeSetUp]
    public async Task ArrancarContenedor() {
        _postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithName("alquiler-storage-postgres")
            .WithDatabase("storage_tests")
            .Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await _postgres.StartAsync(timeout.Token);
        var conexion = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) {
            Host = "host.docker.internal"
        };
        var archivos = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "StorageContainer"));
        archivos.Exists.Should().BeTrue("la compilación debe copiar el proyecto auxiliar de tests");
        _container = new ContainerBuilder("mcr.microsoft.com/dotnet/sdk:10.0")
            .WithName("alquiler-storage-tests")
            .WithResourceMapping(DirectoryPath.Of(archivos.FullName), DirectoryPath.Of("/tests/"), uid: 1654, gid: 1654)
            .WithWorkingDirectory("/tests")
            // Docker Desktop no siempre publica host.docker.internal dentro de los contenedores.
            .WithExtraHost("host.docker.internal", "host-gateway")
            .WithEnvironment("STORAGE_TEST_CONTAINER", "1")
            .WithEnvironment("STORAGE_TEST_CONNECTION", conexion.ConnectionString)
            .WithEnvironment("DOTNET_CLI_UI_LANGUAGE", "en")
            .WithEnvironment("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1")
            .WithEnvironment("DOTNET_CLI_HOME", "/tmp")
            .WithEnvironment("HOME", "/tmp")
            .WithCreateParameterModifier(parameters => parameters.User = "1654:1654")
            .WithCommand("sleep", "infinity")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("dotnet", "--info"))
            .Build();
        await _container.StartAsync(timeout.Token);
    }

    [TestCase("StorageServiceTests")]
    [TestCase("StorageInitializationExtensionsTests")]
    [TestCase("FotosArchivoControllerTests")]
    [TestCase("VehiculoFotosDescarteTests")]
    public async Task Storage_EscenariosEnContenedor_TodosLosCasosPasan(string clase) {
        // Arrange
        var destino = $"/tmp/results/{clase}";
        var comando = new[] {
            "dotnet", "vstest", "/tests/Alquiler-Vehiculos.Storage.Tests.dll",
            $"--TestCaseFilter:FullyQualifiedName~{clase}", $"--ResultsDirectory:{destino}",
            "--logger:trx;LogFileName=tests.trx", "--collect:XPlat Code Coverage",
            "--TestAdapterPath:/tests/Collector", "--Settings:/tests/storage.runsettings"
        };

        // Act
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var resultado = await _container!.ExecAsync(comando, timeout.Token);

        // Assert
        TestContext.Out.WriteLine(resultado.Stdout);
        TestContext.Out.WriteLine(resultado.Stderr);
        // Guardar TRX y cobertura antes de comprobar el resultado permite investigar fallos.
        var trx = await _container.ReadFileAsync($"{destino}/tests.trx", timeout.Token);
        // El script de cobertura reúne host y Docker en una ejecución aislada.
        var resultados = Environment.GetEnvironmentVariable("ALQUILER_TEST_RESULTS_DIRECTORY")
            ?? Path.Combine(TestContext.CurrentContext.WorkDirectory, "TestResults");
        var carpeta = Path.Combine(resultados, "StorageContainer", Guid.NewGuid().ToString("N"), clase);
        Directory.CreateDirectory(carpeta);
        await File.WriteAllBytesAsync(Path.Combine(carpeta, "tests.trx"), trx, timeout.Token);
        TestContext.AddTestAttachment(Path.Combine(carpeta, "tests.trx"));
        // VSTest guarda el adjunto en un subdirectorio GUID y puede crear otra copia interna más profunda.
        var busqueda = await _container.ExecAsync(new[] { "find", destino, "-maxdepth", "2", "-name", "coverage.cobertura.xml" }, timeout.Token);
        busqueda.ExitCode.Should().Be(0);
        var rutas = busqueda.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        rutas.Should().ContainSingle("se debe generar cobertura dentro del contenedor");
        var cobertura = await _container.ReadFileAsync(rutas[0], timeout.Token);
        using var contenidoCobertura = new MemoryStream(cobertura);
        var informeCobertura = XDocument.Load(contenidoCobertura);
        var claseMedida = clase switch {
            "StorageServiceTests" => "Services.Storage.StorageService",
            "FotosArchivoControllerTests" => "Controllers.Vehiculo.FotosController",
            "VehiculoFotosDescarteTests" => "Controllers.Vehiculo.VehiculoController",
            _ => "Infrastructure.StorageInitializationExtensions"
        };
        informeCobertura.Descendants("class").Should().Contain(c =>
            (string?)c.Attribute("name") == "Alquiler_Vehiculos." + claseMedida);
        informeCobertura.Descendants("line").Should().Contain(l => (int)l.Attribute("hits")! > 0,
            "un XML vacío no demuestra que se haya medido la cobertura");
        var archivoCobertura = Path.Combine(carpeta, "coverage.cobertura.xml");
        await File.WriteAllBytesAsync(archivoCobertura, cobertura, timeout.Token);
        TestContext.AddTestAttachment(archivoCobertura);

        resultado.ExitCode.Should().Be(0, "los tests internos deben pasar: {0}\n{1}", resultado.Stdout, resultado.Stderr);
        using var contenidoTrx = new MemoryStream(trx);
        var informe = XDocument.Load(contenidoTrx);
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        var contador = informe.Descendants(ns + "Counters").Single();
        var total = (int)contador.Attribute("total")!;
        total.Should().BeGreaterThan(0, "cero tests no demuestra éxito");
        ((int)contador.Attribute("passed")!).Should().Be(total);
        ((int)contador.Attribute("failed")!).Should().Be(0);
    }

    [OneTimeTearDown]
    public async Task EliminarContenedor() {
        try {
            if (_container is not null) await _container.DisposeAsync();
        } finally {
            if (_postgres is not null) await _postgres.DisposeAsync();
        }
    }
}
