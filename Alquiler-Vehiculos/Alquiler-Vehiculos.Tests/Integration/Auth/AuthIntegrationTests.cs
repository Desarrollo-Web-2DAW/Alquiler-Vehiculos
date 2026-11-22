using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Alquiler_Vehiculos.Tests.Integration.Auth;

/// <summary>
/// Pruebas de integración de autenticación y perfiles contra PostgreSQL real
/// en contenedor (Testcontainers + WebApplicationFactory, RNF-04 y RF-46).
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class AuthIntegrationTests {
    private const string PostgresImage = "postgres:17-alpine";

    private readonly Dictionary<string, string?> _envBackup = new();
    private PostgreSqlContainer? _postgres;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public async Task OneTimeSetUp() {
        _postgres = new PostgreSqlBuilder(PostgresImage)
            .WithDatabase("alquiler_test")
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

        // Fijar variables de entorno ANTES de crear WebApplicationFactory para que
        // AddDatabase y AddIdentityConfiguration lean la cadena del contenedor en Program.cs.
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
    public async Task GetMe_SinToken_Devuelve401Unauthorized() {
        // Arrange
        _client!.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SignupYGetMe_FlujoCompleto_Devuelve201Y200ConPerfil() {
        // Arrange
        var signup = new SignupDto {
            Email = "nuevo.cliente@test.local",
            Password = "Password123!",
            Nombre = "Luis",
            Apellidos = "Vega",
            Telefono = "655443322",
            FechaNacimiento = new DateOnly(1996, 8, 20),
            Dni = "87654321X"
        };

        // Act
        var signupResponse = await _client!.PostAsJsonAsync("/api/auth/signup", signup);
        var auth = await signupResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        var meResponse = await _client!.SendAsync(meRequest);
        var perfil = await meResponse.Content.ReadFromJsonAsync<UserDto>();

        // Assert
        signupResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        perfil!.Email.Should().Be(signup.Email);
        perfil.Role.Should().Be("Cliente");
    }

    [Test]
    public async Task Login_ClienteDadoDeBajaLogica_Devuelve403Forbidden() {
        // Arrange: damos de baja lógica al cliente sembrado (RF-13)
        using (var scope = _factory!.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContextUsuarios>();
            var cliente = await db.Clientes.FirstAsync(c => c.Email == "cliente@test.local");
            cliente.Activo = false;
            await db.SaveChangesAsync();
        }

        var login = new LoginDto {
            Email = "cliente@test.local",
            Password = "Cliente12345!"
        };

        // Act
        var response = await _client!.PostAsJsonAsync("/api/auth/login", login);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
