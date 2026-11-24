using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Alquiler_Vehiculos.Dto.Usuarios;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Alquiler_Vehiculos.Tests.Integration.Auth;

/// <summary>
/// Pruebas de integración de autorización por roles (401/403/200) y administración
/// de cuentas en <c>/api/users</c> contra PostgreSQL real (Testcontainers + WebApplicationFactory, RF-46).
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class UsersIntegrationTests {
    private const string PostgresImage = "postgres:17-alpine";

    private readonly Dictionary<string, string?> _envBackup = new();
    private PostgreSqlContainer? _postgres;
    private readonly CatalogoContenedores _catalogo = new();
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public async Task OneTimeSetUp() {
        _postgres = new PostgreSqlBuilder(PostgresImage)
            .WithDatabase("alquiler_users_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _postgres.StartAsync();

        var connectionString = _postgres.GetConnectionString();
        await _catalogo.StartAsync(connectionString);

        var overrides = new Dictionary<string, string?> {
            ["ConnectionStrings__DefaultConnection"] = connectionString,
            ["Jwt__Key"] = "clave-solo-para-tests-integracion-minimo-32-bytes!",
            ["Jwt__Issuer"] = "Alquiler-Vehiculos",
            ["Jwt__Audience"] = "Alquiler-Vehiculos-Users",
            ["Jwt__ExpirationMinutes"] = "30",
            ["Email__UseSmtp"] = "false",
            ["Seed__AdminEmail"] = "admin@test.local",
            ["Seed__AdminPassword"] = "Admin12345!",
            ["Seed__ClienteEmail"] = "cliente@test.local",
            ["Seed__ClientePassword"] = "Cliente12345!",
            ["Seed__TrabajadorEmail"] = "trabajador@test.local",
            ["Seed__TrabajadorPassword"] = "Trabajador12345!",
            ["Seed__MecanicoEmail"] = "mecanico@test.local",
            ["Seed__MecanicoPassword"] = "Mecanico12345!",
            ["Seed__ResponsableEmail"] = "responsable@test.local",
            ["Seed__ResponsablePassword"] = "Responsable12345!"
        };

        foreach (var (key, value) in _catalogo.VariablesDeEntorno()) overrides[key] = value;

        foreach (var (key, value) in overrides) {
            _envBackup[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, config) => {
                    config.AddInMemoryCollection(new Dictionary<string, string?> {
                        ["ConnectionStrings:DefaultConnection"] = connectionString,
                        ["Jwt:Key"] = "clave-solo-para-tests-integracion-minimo-32-bytes!",
                        ["Email:UseSmtp"] = "false"
                    });
                });
            });

        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() {
        _client?.Dispose();

        if (_factory is not null) {
            await _factory.DisposeAsync();
        }

        if (_postgres is not null) {
            await _postgres.DisposeAsync();
        }

        await _catalogo.DisposeAsync();

        foreach (var (key, originalValue) in _envBackup) {
            Environment.SetEnvironmentVariable(key, originalValue);
        }
    }

    [Test]
    public async Task GetAll_SinToken_Devuelve401Unauthorized() {
        // Act
        var response = await _client!.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestCase("cliente@test.local", "Cliente12345!")]
    [TestCase("trabajador@test.local", "Trabajador12345!")]
    [TestCase("mecanico@test.local", "Mecanico12345!")]
    [TestCase("responsable@test.local", "Responsable12345!")]
    public async Task GetAll_ConRolNoAdmin_Devuelve403Forbidden(string email, string password) {
        // Arrange
        var token = await LoginAsync(email, password);
        using var request = CrearPeticion(HttpMethod.Get, "/api/users", token);

        // Act
        var response = await _client!.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetAll_ConTokenAdmin_Devuelve200ConLos5PerfilesSemilla() {
        // Arrange
        var token = await LoginAsync("admin@test.local", "Admin12345!");
        using var request = CrearPeticion(HttpMethod.Get, "/api/users", token);

        // Act
        var response = await _client!.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var usuarios = await response.Content.ReadFromJsonAsync<List<UserDetalleDto>>();
        usuarios.Should().NotBeNull();
        usuarios!.Should().HaveCountGreaterThanOrEqualTo(5);
        usuarios.Select(u => u.Email).Should().Contain([
            "admin@test.local",
            "cliente@test.local",
            "trabajador@test.local",
            "mecanico@test.local",
            "responsable@test.local"
        ]);
    }

    [Test]
    public async Task GetById_Inexistente_Devuelve404NotFound() {
        // Arrange
        var token = await LoginAsync("admin@test.local", "Admin12345!");
        using var request = CrearPeticion(HttpMethod.Get, $"/api/users/{Guid.NewGuid()}", token);

        // Act
        var response = await _client!.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CambiarRol_TrabajadorAResponsableYClienteAAdmin_AplicaReglasDeNegocio() {
        // Arrange
        var adminToken = await LoginAsync("admin@test.local", "Admin12345!");
        var usuarios = await ListarUsuariosAsync(adminToken);
        var trabajador = usuarios.First(u => u.Email == "trabajador@test.local");
        var cliente = usuarios.First(u => u.Email == "cliente@test.local");

        // Act 1: Trabajador -> Responsable (permitido)
        using var reqValido = CrearPeticionJson(
            HttpMethod.Patch, $"/api/users/{trabajador.Id}/role", adminToken, new CambiarRolDto { Role = "Responsable" });
        var respValido = await _client!.SendAsync(reqValido);

        // Act 2: Cliente -> Admin (incompatible con persona -> 409)
        using var reqIncompatible = CrearPeticionJson(
            HttpMethod.Patch, $"/api/users/{cliente.Id}/role", adminToken, new CambiarRolDto { Role = "Admin" });
        var respIncompatible = await _client!.SendAsync(reqIncompatible);

        // Restore rol original del trabajador para no afectar otros tests
        using var reqRestore = CrearPeticionJson(
            HttpMethod.Patch, $"/api/users/{trabajador.Id}/role", adminToken, new CambiarRolDto { Role = "Trabajador" });
        await _client!.SendAsync(reqRestore);

        // Assert
        respValido.StatusCode.Should().Be(HttpStatusCode.OK);
        var actualizado = await respValido.Content.ReadFromJsonAsync<UserDetalleDto>();
        actualizado!.Role.Should().Be("Responsable");

        respIncompatible.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var cuerpoError = await respIncompatible.Content.ReadAsStringAsync();
        cuerpoError.Should().Contain("ROL_INCOMPATIBLE_CON_PERSONA");
    }

    [Test]
    public async Task DesactivarYReactivar_UltimoAdminYCliente_ProtegeUltimoAdminYPermiteBajaYAlta() {
        // Arrange
        var adminToken = await LoginAsync("admin@test.local", "Admin12345!");
        var usuarios = await ListarUsuariosAsync(adminToken);
        var admin = usuarios.First(u => u.Email == "admin@test.local");
        var cliente = usuarios.First(u => u.Email == "cliente@test.local");

        // Act 1: Intentar desactivar al último admin activo -> 409 ULTIMO_ADMIN
        using var reqAdmin = CrearPeticion(HttpMethod.Delete, $"/api/users/{admin.Id}", adminToken);
        var respAdmin = await _client!.SendAsync(reqAdmin);

        // Act 2: Desactivar cliente -> 204 No Content
        using var reqDelete = CrearPeticion(HttpMethod.Delete, $"/api/users/{cliente.Id}", adminToken);
        var respDelete = await _client!.SendAsync(reqDelete);

        // Act 3: Login del cliente desactivado falla con 401 (IsDeleted)
        var respLoginDesactivado = await _client!.PostAsJsonAsync("/api/auth/login", new LoginDto {
            Email = "cliente@test.local",
            Password = "Cliente12345!"
        });

        // Act 4: Reactivar cliente -> 200 OK con IsDeleted = false
        using var reqReactivar = CrearPeticion(HttpMethod.Patch, $"/api/users/{cliente.Id}/reactivate", adminToken);
        var respReactivar = await _client!.SendAsync(reqReactivar);

        // Assert
        respAdmin.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await respAdmin.Content.ReadAsStringAsync()).Should().Contain("ULTIMO_ADMIN");

        respDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);
        respLoginDesactivado.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        respReactivar.StatusCode.Should().Be(HttpStatusCode.OK);
        var reactivado = await respReactivar.Content.ReadFromJsonAsync<UserDetalleDto>();
        reactivado!.IsDeleted.Should().BeFalse();
        reactivado.DeletedAt.Should().BeNull();
    }

    private async Task<string> LoginAsync(string email, string password) {
        var response = await _client!.PostAsJsonAsync("/api/auth/login", new LoginDto {
            Email = email,
            Password = password
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Login fallido para {email}");
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return auth!.Token;
    }

    private async Task<List<UserDetalleDto>> ListarUsuariosAsync(string adminToken) {
        using var req = CrearPeticion(HttpMethod.Get, "/api/users", adminToken);
        var resp = await _client!.SendAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await resp.Content.ReadFromJsonAsync<List<UserDetalleDto>>())!;
    }

    private static HttpRequestMessage CrearPeticion(HttpMethod method, string url, string token) {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static HttpRequestMessage CrearPeticionJson<T>(HttpMethod method, string url, string token, T body) {
        var request = CrearPeticion(method, url, token);
        request.Content = JsonContent.Create(body);
        return request;
    }
}
