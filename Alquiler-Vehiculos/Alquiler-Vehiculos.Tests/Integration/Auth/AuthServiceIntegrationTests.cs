using System.IdentityModel.Tokens.Jwt;
using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Models.Usuarios;
using Alquiler_Vehiculos.Repositories.Clientes;
using Alquiler_Vehiculos.Services.Usuarios.Auth;
using Alquiler_Vehiculos.Validators.Usuarios;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Testcontainers.PostgreSql;

namespace Alquiler_Vehiculos.Tests.Integration.Auth;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class AuthServiceIntegrationTests {
    private PostgreSqlContainer? _container;
    private ServiceProvider _provider = null!;
    private IConfiguration _configuration = null!;
    // Valores efímeros, generados exclusivamente para esta base de pruebas.
    private readonly string _password = Guid.NewGuid() + "aA1!";

    private sealed class EntornoPruebas : IWebHostEnvironment {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "Alquiler-Vehiculos.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }

    [OneTimeSetUp]
    public async Task ArrancarPostgreSql() {
        _container = new PostgreSqlBuilder("postgres:17-alpine").WithDatabase("auth_tests").Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await _container.StartAsync(timeout.Token);
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Jwt:Key"] = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            ["Jwt:Issuer"] = "alquiler-tests", ["Jwt:Audience"] = "alquiler-tests", ["Jwt:ExpirationMinutes"] = "30",
            ["Seed:AdminEmail"] = "admin@example.com", ["Seed:AdminPassword"] = _password,
            ["Seed:ClienteEmail"] = "cliente@example.com", ["Seed:ClientePassword"] = _password,
            ["Seed:TrabajadorEmail"] = "trabajador@example.com", ["Seed:TrabajadorPassword"] = _password
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContextUsuarios>(o => o.UseNpgsql(_container.GetConnectionString()));
        services.AddIdentityConfiguration(_configuration, new EntornoPruebas());
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IValidator<SignupDto>, SignupValidator>();
        services.AddScoped<IValidator<LoginDto>, LoginValidator>();
        services.AddScoped<IAuthService, AuthService>();
        _provider = services.BuildServiceProvider();
        using var scope = _provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContextUsuarios>().Database.MigrateAsync();
    }

    [SetUp]
    public async Task ReiniciarDatos() {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContextUsuarios>();
        // La oficina semilla referencia a su responsable: se borra antes que los trabajadores.
        await db.Oficinas.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();
        await db.Clientes.ExecuteDeleteAsync();
        await db.Trabajadores.ExecuteDeleteAsync();
        await IdentitySeeder.SeedAsync(_provider, _configuration);
    }

    [OneTimeTearDown]
    public async Task LiberarRecursos() {
        try { if (_provider is not null) await _provider.DisposeAsync(); }
        finally { if (_container is not null) await _container.DisposeAsync(); }
    }

    private SignupDto CrearAlta() => new() {
        Email = "nuevo@example.com", Password = _password, Nombre = " Lucia ", Apellidos = " Fuertes ",
        Telefono = "600123456", FechaNacimiento = new(2000, 1, 1), Dni = " 87654321x "
    };

    [TestCase("admin@example.com", "Admin")]
    [TestCase("cliente@example.com", "Cliente")]
    [TestCase("trabajador@example.com", "Trabajador")]
    public async Task Login_CuentaActiva_DevuelveJwtConRol(string email, string rol) {
        // Arrange
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthService>();

        // Act
        var resultado = await service.LoginAsync(new LoginDto { Email = email, Password = _password });

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Email.Should().Be(email);
        resultado.Value.Role.Should().Be(rol);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(resultado.Value.Token);
        token.Issuer.Should().Be("alquiler-tests");
        token.Audiences.Should().ContainSingle().Which.Should().Be("alquiler-tests");
        token.Claims.Should().Contain(c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == rol);
        resultado.Value.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromSeconds(10));
    }

    [TestCase("ausente@example.com", false)]
    [TestCase("cliente@example.com", false)]
    [TestCase("cliente@example.com", true)]
    public async Task Login_CredencialesIncorrectasOCuentaBorrada_DevuelveErrorGenerico(string email, bool borrada) {
        // Arrange
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContextUsuarios>();
        if (borrada) {
            var user = await db.Users.SingleAsync(u => u.Email == email);
            user.IsDeleted = true;
            await db.SaveChangesAsync();
        }
        var service = scope.ServiceProvider.GetRequiredService<IAuthService>();

        // Act
        var resultado = await service.LoginAsync(new LoginDto { Email = email, Password = borrada ? _password : "Incorrecta123!" });

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Code.Should().Be("CREDENCIALES_INVALIDAS");
    }

    [TestCase("bloqueada", "CUENTA_BLOQUEADA")]
    [TestCase("sinRol", "CUENTA_SIN_ROL")]
    [TestCase("inactiva", "CUENTA_INACTIVA")]
    [TestCase("personaBorrada", "CUENTA_INACTIVA")]
    public async Task Login_CuentaNoHabilitada_DevuelveErrorConcreto(string caso, string codigo) {
        // Arrange
        using var scope = _provider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = (await manager.FindByEmailAsync("cliente@example.com"))!;
        if (caso == "bloqueada") {
            (await manager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(5))).Succeeded.Should().BeTrue();
        } else if (caso == "sinRol") {
            (await manager.RemoveFromRoleAsync(user, "Cliente")).Succeeded.Should().BeTrue();
        } else {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContextUsuarios>();
            var cliente = await db.Clientes.SingleAsync();
            cliente.Activo = caso != "inactiva";
            cliente.IsDeleted = caso == "personaBorrada";
            await db.SaveChangesAsync();
        }
        var service = scope.ServiceProvider.GetRequiredService<IAuthService>();

        // Act
        var resultado = await service.LoginAsync(new LoginDto { Email = user.Email!, Password = _password });

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Code.Should().Be(codigo);
    }

    [Test]
    public async Task SignUpAsync_DatosValidos_PersistePersonaCuentaYRolNormalizados() {
        // Arrange
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var dto = CrearAlta();

        // Act
        var resultado = await service.SignUpAsync(dto);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Role.Should().Be("Cliente");
        using var lectura = _provider.CreateScope();
        var db = lectura.ServiceProvider.GetRequiredService<AppDbContextUsuarios>();
        var user = await db.Users.Include(u => u.Cliente).SingleAsync(u => u.Email == dto.Email);
        user.Cliente.Should().NotBeNull();
        user.Cliente!.Nombre.Should().Be("Lucia");
        user.Cliente.Apellidos.Should().Be("Fuertes");
        user.Cliente.Dni.Should().Be("87654321X");
        user.Cliente.Nie.Should().BeNull();
        (await lectura.ServiceProvider.GetRequiredService<UserManager<User>>().GetRolesAsync(user))
            .Should().ContainSingle().Which.Should().Be("Cliente");
    }

    [TestCase("email", "EMAIL_DUPLICADO")]
    [TestCase("documento", "DOCUMENTO_DUPLICADO")]
    [TestCase("validacion", "VALIDACION")]
    public async Task SignUpAsync_DatosRechazados_NoPersisteCuenta(string caso, string codigo) {
        // Arrange
        using var scope = _provider.CreateScope();
        var dto = CrearAlta();
        dto = caso switch {
            "email" => dto with { Email = "cliente@example.com" },
            "documento" => dto with { Dni = "12345678Z" }, // DNI del cliente semilla
            _ => dto with { Password = "", Dni = null }
        };
        var service = scope.ServiceProvider.GetRequiredService<IAuthService>();

        // Act
        var resultado = await service.SignUpAsync(dto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Code.Should().Be(codigo);
        using var lectura = _provider.CreateScope();
        (await lectura.ServiceProvider.GetRequiredService<AppDbContextUsuarios>().Users.CountAsync()).Should().Be(3);
    }

    [TestCase("admin@example.com", "", "Admin")]
    [TestCase("cliente@example.com", "Ana", "Cliente")]
    [TestCase("trabajador@example.com", "Carlos", "Trabajador")]
    public async Task GetMeAsync_CuentaExistente_DevuelvePersonaYRol(string email, string nombre, string rol) {
        // Arrange
        using var scope = _provider.CreateScope();
        var user = (await scope.ServiceProvider.GetRequiredService<UserManager<User>>().FindByEmailAsync(email))!;
        var service = scope.ServiceProvider.GetRequiredService<IAuthService>();

        // Act
        var resultado = await service.GetMeAsync(user.Id);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeEquivalentTo(new { user.Id, Email = email, Nombre = nombre, Role = rol });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetMeAsync_CuentaAusenteOBorrada_DevuelveNoEncontrado(bool borrada) {
        // Arrange
        using var scope = _provider.CreateScope();
        var id = Guid.NewGuid();
        if (borrada) {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContextUsuarios>();
            var user = await db.Users.FirstAsync();
            id = user.Id;
            user.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        // Act
        var resultado = await scope.ServiceProvider.GetRequiredService<IAuthService>().GetMeAsync(id);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Code.Should().Be("USUARIO_NO_ENCONTRADO");
        resultado.Error.Message.Should().Contain(id.ToString());
    }

    [Test]
    public async Task SeedAsync_SegundaEjecucion_NoDuplicaPersonasCuentasNiRoles() {
        // Arrange
        // SetUp ya ha sembrado las cuentas en la base aislada.

        // Act
        await IdentitySeeder.SeedAsync(_provider, _configuration);

        // Assert
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContextUsuarios>();
        (await db.Users.CountAsync()).Should().Be(3);
        (await db.Clientes.CountAsync()).Should().Be(1);
        (await db.Trabajadores.CountAsync()).Should().Be(1);
        (await db.Roles.CountAsync()).Should().Be(4);
        (await db.UserRoles.CountAsync()).Should().Be(3);
    }

    [Test]
    public async Task GetMeAsync_CuentaSinEmailPersonaNiRol_DevuelveCamposVacios() {
        // Arrange
        Guid id;
        using (var preparacion = _provider.CreateScope()) {
            var db = preparacion.ServiceProvider.GetRequiredService<AppDbContextUsuarios>();
            var admin = await db.Users.SingleAsync(u => u.Email == "admin@example.com");
            id = admin.Id;
            admin.Email = null;
            admin.NormalizedEmail = null;
            await db.UserRoles.Where(r => r.UserId == id).ExecuteDeleteAsync();
            await db.SaveChangesAsync();
        }
        using var lectura = _provider.CreateScope();
        var service = lectura.ServiceProvider.GetRequiredService<IAuthService>();

        // Act
        var resultado = await service.GetMeAsync(id);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().Be(new UserDto(id, "", "", "", ""));
    }
}
