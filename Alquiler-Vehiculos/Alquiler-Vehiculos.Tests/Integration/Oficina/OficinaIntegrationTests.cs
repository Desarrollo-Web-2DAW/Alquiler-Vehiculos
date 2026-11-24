using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Alquiler_Vehiculos.Dto.Oficina;
using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Services.Oficina;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Alquiler_Vehiculos.Tests.Integration.Oficina;

/// <summary>
/// Pruebas de integración del módulo de Oficina única contra PostgreSQL real
/// (Testcontainers + WebApplicationFactory, RF-11, CU-18, CU-22).
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class OficinaIntegrationTests {
    private const string PostgresImage = "postgres:17-alpine";

    private readonly Dictionary<string, string?> _envBackup = new();
    private PostgreSqlContainer? _postgres;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public async Task OneTimeSetUp() {
        _postgres = new PostgreSqlBuilder(PostgresImage)
            .WithDatabase("alquiler_oficina_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _postgres.StartAsync();

        var connectionString = _postgres.GetConnectionString();

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

        foreach (var (key, originalValue) in _envBackup) {
            Environment.SetEnvironmentVariable(key, originalValue);
        }
    }

    [Test]
    public async Task Obtener_SinToken_Devuelve200ConOficinaSemillaYResponsable() {
        // Act
        var response = await _client!.GetAsync("/api/oficina");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var oficina = await response.Content.ReadFromJsonAsync<OficinaResponseDto>();
        oficina.Should().NotBeNull();
        oficina!.Nombre.Should().NotBeNullOrWhiteSpace();
        oficina.Ciudad.Should().Be("Leganés");
        oficina.CodigoPostal.Should().Be("28911");
        oficina.Localizacion.Latitud.Should().BeApproximately(40.3319, 0.001);
        oficina.Localizacion.Longitud.Should().BeApproximately(-3.7635, 0.001);
        oficina.HorarioFormateado.Should().Be("Lunes a Viernes de 09:00 a 20:00");
        oficina.Responsable.Email.Should().Be("responsable@test.local");
    }

    [Test]
    public async Task CambiarResponsable_SinToken_Devuelve401Unauthorized() {
        // Act
        var response = await _client!.PatchAsJsonAsync(
            "/api/oficina/responsable",
            new CambiarResponsableDto { TrabajadorId = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestCase("cliente@test.local", "Cliente12345!")]
    [TestCase("trabajador@test.local", "Trabajador12345!")]
    [TestCase("mecanico@test.local", "Mecanico12345!")]
    [TestCase("responsable@test.local", "Responsable12345!")]
    public async Task CambiarResponsable_ConRolNoAdmin_Devuelve403Forbidden(string email, string password) {
        // Arrange
        var token = await LoginAsync(email, password);
        using var request = CrearPeticionJson(
            HttpMethod.Patch, "/api/oficina/responsable", token, new CambiarResponsableDto { TrabajadorId = Guid.NewGuid() });

        // Act
        var response = await _client!.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task CambiarResponsable_MecanicoYOperaciones_RechazaMecanico422YPermiteOperaciones200() {
        // Arrange
        var adminToken = await LoginAsync("admin@test.local", "Admin12345!");
        Guid mecanicoId;
        Guid trabajadorOpId;
        Guid responsableOriginalId;

        using (var scope = _factory!.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContextUsuarios>();
            mecanicoId = (await db.Trabajadores.FirstAsync(t => t.Email == "mecanico@test.local")).Id;
            trabajadorOpId = (await db.Trabajadores.FirstAsync(t => t.Email == "trabajador@test.local")).Id;
            responsableOriginalId = (await db.Trabajadores.FirstAsync(t => t.Email == "responsable@test.local")).Id;
        }

        // Act 1: Mecánico -> 422 RESPONSABLE_DEBE_SER_OPERACIONES
        using var reqMecanico = CrearPeticionJson(
            HttpMethod.Patch, "/api/oficina/responsable", adminToken, new CambiarResponsableDto { TrabajadorId = mecanicoId });
        var respMecanico = await _client!.SendAsync(reqMecanico);

        // Act 2: Trabajador de Operaciones -> 200 OK y sincroniza EsResponsableAsync
        using var reqOp = CrearPeticionJson(
            HttpMethod.Patch, "/api/oficina/responsable", adminToken, new CambiarResponsableDto { TrabajadorId = trabajadorOpId });
        var respOp = await _client!.SendAsync(reqOp);

        bool nuevoEsResponsable;
        bool anteriorSigueSiendoResponsable;
        using (var scope = _factory!.Services.CreateScope()) {
            var oficinaService = scope.ServiceProvider.GetRequiredService<IOficinaService>();
            nuevoEsResponsable = await oficinaService.EsResponsableAsync(trabajadorOpId);
            anteriorSigueSiendoResponsable = await oficinaService.EsResponsableAsync(responsableOriginalId);
        }

        // Restore responsable original
        using var reqRestore = CrearPeticionJson(
            HttpMethod.Patch, "/api/oficina/responsable", adminToken, new CambiarResponsableDto { TrabajadorId = responsableOriginalId });
        await _client!.SendAsync(reqRestore);

        // Assert
        respMecanico.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await respMecanico.Content.ReadAsStringAsync()).Should().Contain("RESPONSABLE_DEBE_SER_OPERACIONES");

        respOp.StatusCode.Should().Be(HttpStatusCode.OK);
        var relevada = await respOp.Content.ReadFromJsonAsync<OficinaResponseDto>();
        relevada!.Responsable.Email.Should().Be("trabajador@test.local");
        nuevoEsResponsable.Should().BeTrue();
        anteriorSigueSiendoResponsable.Should().BeFalse();
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

    private static HttpRequestMessage CrearPeticionJson<T>(HttpMethod method, string url, string token, T body) {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(body);
        return request;
    }
}
