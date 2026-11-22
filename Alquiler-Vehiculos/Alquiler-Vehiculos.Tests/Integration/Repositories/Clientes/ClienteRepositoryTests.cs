using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Models.Usuarios.Clientes;
using Alquiler_Vehiculos.Repositories.Clientes;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Alquiler_Vehiculos.Tests.Integration.Repositories.Clientes;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class ClienteRepositoryTests {
    private PostgreSqlContainer? _container;
    private DbContextOptions<AppDbContextUsuarios> _options = null!;

    [OneTimeSetUp]
    public async Task ArrancarPostgreSql() {
        _container = new PostgreSqlBuilder("postgres:17-alpine").WithDatabase("clientes_tests").Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await _container.StartAsync(timeout.Token);
        _options = new DbContextOptionsBuilder<AppDbContextUsuarios>().UseNpgsql(_container.GetConnectionString()).Options;
        await using var db = new AppDbContextUsuarios(_options);
        await db.Database.MigrateAsync();
    }

    [SetUp]
    public async Task LimpiarDatos() {
        await using var db = new AppDbContextUsuarios(_options);
        await db.Clientes.ExecuteDeleteAsync();
    }

    [OneTimeTearDown]
    public async Task EliminarPostgreSql() {
        if (_container is not null) await _container.DisposeAsync();
    }

    [TestCase("12345678Z", null, null, true)]
    [TestCase(null, "X1234567L", null, true)]
    [TestCase(null, null, "ABC123", true)]
    [TestCase("otro", "X1234567L", null, true)]
    [TestCase("otro", "otro", "ABC123", true)]
    [TestCase("otro", "otro", "otro", false)]
    [TestCase(null, null, null, false)]
    [TestCase("", " ", "", false)]
    public async Task DocumentoEnUsoAsync_DocumentosIndependientes_DetectaSoloCoincidencias(
        string? dni, string? nie, string? pasaporte, bool esperado) {
        // Arrange
        await using (var preparacion = new AppDbContextUsuarios(_options)) {
            preparacion.Clientes.Add(new Cliente {
                Nombre = "Lucia", Apellidos = "Fuertes", Email = "cliente@example.com", Telefono = "600123456",
                FechaNacimiento = new(2000, 1, 1), Dni = "12345678Z", Nie = "X1234567L", Pasaporte = "ABC123"
            });
            await preparacion.SaveChangesAsync();
        }
        await using var db = new AppDbContextUsuarios(_options);
        var repository = new ClienteRepository(db);

        // Act
        var resultado = await repository.DocumentoEnUsoAsync(dni, nie, pasaporte);

        // Assert
        resultado.Should().Be(esperado);
    }

    [Test]
    public async Task DocumentoEnUsoAsync_ClienteDadoDeBaja_ConservaDocumentoOcupado() {
        // Arrange
        await using (var preparacion = new AppDbContextUsuarios(_options)) {
            preparacion.Clientes.Add(new Cliente {
                Email = "baja@example.com", Telefono = "600123456", Dni = "12345678Z",
                Activo = false, IsDeleted = true, DeletedAt = DateTime.UtcNow
            });
            await preparacion.SaveChangesAsync();
        }
        await using var db = new AppDbContextUsuarios(_options);
        var repository = new ClienteRepository(db);

        // Act
        var resultado = await repository.DocumentoEnUsoAsync("12345678Z", null, null);

        // Assert
        resultado.Should().BeTrue();
    }
}
