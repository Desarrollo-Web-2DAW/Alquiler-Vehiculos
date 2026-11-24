using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Models.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios.Clientes;
using Alquiler_Vehiculos.Repositories.Clientes;
using Alquiler_Vehiculos.Services.Usuarios.Auth;
using Alquiler_Vehiculos.Validators.Usuarios;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace Alquiler_Vehiculos.Tests.Integration.Auth;

/// <summary>
/// Tests de integración de AuthService contra PostgreSQL real con Testcontainers.
/// Prueba el flujo completo: SignUp, Login y GetMe con UserManager y SignInManager
/// reales contra las tablas de Identity. Requiere Docker.
/// </summary>
[Category("Integration")]
public class AuthServiceIntegrationTests
{
    private static readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("alquiler_identity_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private IServiceScope _scope = null!;

    [OneTimeSetUp]
    public async Task ArrancarPostgreSQL()
    {
        await _container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task PararPostgreSQL()
    {
        await _container.DisposeAsync();
    }

    [SetUp]
    public async Task Setup()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder.AddConsole());

        services.AddDbContext<AppDbContextUsuarios>(options =>
            options.UseNpgsql(_container.GetConnectionString()));

        services.AddAuthentication();

        services.AddIdentityCore<User>()
            .AddRoles<IdentityRole<Guid>>()
            .AddSignInManager()
            .AddEntityFrameworkStores<AppDbContextUsuarios>();

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<FluentValidation.IValidator<SignupDto>, SignupValidator>();
        services.AddScoped<FluentValidation.IValidator<LoginDto>, LoginValidator>();
        services.AddScoped<AuthService>();

        var jwtSettings = new JwtSettings
        {
            Key = "EstaEsUnaClaveSecretaMuyLargaParaPruebasDeJwtToken123",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpirationMinutes = 30
        };
        services.AddSingleton(Options.Create(jwtSettings));

        var provider = services.BuildServiceProvider();

        // Crear esquema y roles
        using (var setupScope = provider.CreateScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<AppDbContextUsuarios>();
            await db.Database.EnsureCreatedAsync();

            var roleManager = setupScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            if (!await roleManager.RoleExistsAsync("Cliente"))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>("Cliente"));
            }
        }

        _scope = provider.CreateScope();
    }

    [TearDown]
    public async Task TearDown()
    {
        var db = _scope.ServiceProvider.GetRequiredService<AppDbContextUsuarios>();
        await db.Database.EnsureDeletedAsync();
        _scope.Dispose();
    }

    private AuthService GetSut() => _scope.ServiceProvider.GetRequiredService<AuthService>();
    private UserManager<User> GetUserManager() => _scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    #region SignUp

    /// <summary>
    /// Dado un signup con datos válidos, cuando se llama a SignUpAsync,
    /// entonces crea el usuario en la BD y devuelve un token JWT.
    /// </summary>
    [Test]
    public async Task SignUpAsync_DatosValidos_CreaUsuarioYDevuelveToken()
    {
        // Arrange
        var signupDto = new SignupDto
        {
            Email = "carlos@ejemplo.com",
            Password = "Pass123!",
            Nombre = "Carlos",
            Apellidos = "López",
            Telefono = "600123456",
            FechaNacimiento = new DateOnly(1990, 5, 15),
            Dni = "12345678Z"
        };

        // Act
        var result = await GetSut().SignUpAsync(signupDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().NotBeNullOrEmpty();
        result.Value.Email.Should().Be("carlos@ejemplo.com");
        result.Value.Role.Should().Be("Cliente");

        // Verificar persistencia con contexto nuevo
        var db = _scope.ServiceProvider.GetRequiredService<AppDbContextUsuarios>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "carlos@ejemplo.com");
        user.Should().NotBeNull();
        user!.Cliente.Should().NotBeNull();
        user.Cliente!.Nombre.Should().Be("Carlos");
        user.Cliente.Dni.Should().Be("12345678Z");

        var roles = await GetUserManager().GetRolesAsync(user);
        roles.Should().Contain("Cliente");
    }

    /// <summary>
    /// Dado un signup con email duplicado, cuando se llama a SignUpAsync,
    /// entonces devuelve error de email existente.
    /// </summary>
    [Test]
    public async Task SignUpAsync_EmailDuplicado_DevuelveError()
    {
        // Arrange
        var signupDto = new SignupDto
        {
            Email = "duplicado@ejemplo.com",
            Password = "Pass123!",
            Nombre = "Carlos",
            Apellidos = "López",
            Telefono = "600123456",
            FechaNacimiento = new DateOnly(1990, 5, 15),
            Dni = "12345678Z"
        };

        // Crear el primer usuario
        await GetSut().SignUpAsync(signupDto);

        // Act - intentar duplicar
        var result = await GetSut().SignUpAsync(signupDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EMAIL_DUPLICADO");
    }

    /// <summary>
    /// Dado un signup con documento duplicado, cuando se llama a SignUpAsync,
    /// entonces devuelve error de documento existente.
    /// </summary>
    [Test]
    public async Task SignUpAsync_DocumentoDuplicado_DevuelveError()
    {
        // Arrange
        var signupDto1 = new SignupDto
        {
            Email = "primero@ejemplo.com",
            Password = "Pass123!",
            Nombre = "Carlos",
            Apellidos = "López",
            Telefono = "600123456",
            FechaNacimiento = new DateOnly(1990, 5, 15),
            Dni = "12345678Z"
        };

        await GetSut().SignUpAsync(signupDto1);

        var signupDto2 = new SignupDto
        {
            Email = "segundo@ejemplo.com",
            Password = "Pass123!",
            Nombre = "Otro",
            Apellidos = "Usuario",
            Telefono = "600987654",
            FechaNacimiento = new DateOnly(1995, 3, 20),
            Dni = "12345678Z"
        };

        // Act
        var result = await GetSut().SignUpAsync(signupDto2);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DOCUMENTO_DUPLICADO");
    }

    /// <summary>
    /// Dado un signup con contraseña débil, cuando se llama a SignUpAsync,
    /// entonces devuelve error de validación.
    /// </summary>
    [Test]
    public async Task SignUpAsync_ContraseñaDebil_DevuelveErrorValidacion()
    {
        // Arrange
        var signupDto = new SignupDto
        {
            Email = "debil@ejemplo.com",
            Password = "weak",
            Nombre = "Carlos",
            Apellidos = "López",
            Telefono = "600123456",
            FechaNacimiento = new DateOnly(1990, 5, 15),
            Dni = "12345678Z"
        };

        // Act
        var result = await GetSut().SignUpAsync(signupDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("VALIDACION");
    }

    /// <summary>
    /// Dado un signup con NIE, cuando se llama a SignUpAsync,
    /// entonces crea el usuario correctamente.
    /// </summary>
    [Test]
    public async Task SignUpAsync_ConNie_CreaUsuario()
    {
        // Arrange
        var signupDto = new SignupDto
        {
            Email = "nie@ejemplo.com",
            Password = "Pass123!",
            Nombre = "Carlos",
            Apellidos = "López",
            Telefono = "600123456",
            FechaNacimiento = new DateOnly(1990, 5, 15),
            Nie = "x9876543k"
        };

        // Act
        var result = await GetSut().SignUpAsync(signupDto);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var db = _scope.ServiceProvider.GetRequiredService<AppDbContextUsuarios>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "nie@ejemplo.com");
        user.Should().NotBeNull();
        user!.Cliente!.Nie.Should().Be("X9876543K");
    }

    #endregion

    #region Login

    /// <summary>
    /// Dado un usuario registrado, cuando hace login con credenciales correctas,
    /// entonces devuelve un token JWT.
    /// </summary>
    [Test]
    public async Task Login_CredencialesCorrectas_DevuelveToken()
    {
        // Arrange - crear usuario
        var signupDto = new SignupDto
        {
            Email = "login@ejemplo.com",
            Password = "Pass123!",
            Nombre = "Carlos",
            Apellidos = "López",
            Telefono = "600123456",
            FechaNacimiento = new DateOnly(1990, 5, 15),
            Dni = "12345678Z"
        };
        await GetSut().SignUpAsync(signupDto);

        var loginDto = new LoginDto
        {
            Email = "login@ejemplo.com",
            Password = "Pass123!"
        };

        // Act
        var result = await GetSut().LoginAsync(loginDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().NotBeNullOrEmpty();
        result.Value.Email.Should().Be("login@ejemplo.com");
        result.Value.Role.Should().Be("Cliente");
    }

    /// <summary>
    /// Dado un login con contraseña incorrecta, cuando se llama a Login,
    /// entonces devuelve error de credenciales inválidas.
    /// </summary>
    [Test]
    public async Task Login_ContraseñaIncorrecta_DevuelveError()
    {
        // Arrange
        var signupDto = new SignupDto
        {
            Email = "login@ejemplo.com",
            Password = "CorrectPass1!",
            Nombre = "Carlos",
            Apellidos = "López",
            Telefono = "600123456",
            FechaNacimiento = new DateOnly(1990, 5, 15),
            Dni = "12345678Z"
        };
        await GetSut().SignUpAsync(signupDto);

        var loginDto = new LoginDto
        {
            Email = "login@ejemplo.com",
            Password = "WrongPass1!"
        };

        // Act
        var result = await GetSut().LoginAsync(loginDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CREDENCIALES_INVALIDAS");
    }

    /// <summary>
    /// Dado un login con email que no existe, cuando se llama a Login,
    /// entonces devuelve error de credenciales inválidas.
    /// </summary>
    [Test]
    public async Task Login_EmailNoExistente_DevuelveError()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "noexiste@ejemplo.com",
            Password = "Pass123!"
        };

        // Act
        var result = await GetSut().LoginAsync(loginDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CREDENCIALES_INVALIDAS");
    }

    /// <summary>
    /// Dado un login con email vacío, cuando se llama a Login,
    /// entonces devuelve error de validación.
    /// </summary>
    [Test]
    public async Task Login_EmailVacio_DevuelveErrorValidacion()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "",
            Password = "Pass123!"
        };

        // Act
        var result = await GetSut().LoginAsync(loginDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("VALIDACION");
    }

    #endregion

    #region GetMeAsync

    /// <summary>
    /// Dado un usuario registrado, cuando se llama a GetMeAsync con su ID,
    /// entonces devuelve los datos del usuario.
    /// </summary>
    [Test]
    public async Task GetMeAsync_UsuarioExistente_DevuelveDatos()
    {
        // Arrange
        var signupDto = new SignupDto
        {
            Email = "getme@ejemplo.com",
            Password = "Pass123!",
            Nombre = "Carlos",
            Apellidos = "López",
            Telefono = "600123456",
            FechaNacimiento = new DateOnly(1990, 5, 15),
            Dni = "12345678Z"
        };
        var signupResult = await GetSut().SignUpAsync(signupDto);
        var userId = (await GetUserManager().FindByEmailAsync("getme@ejemplo.com"))!.Id;

        // Act
        var result = await GetSut().GetMeAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(userId);
        result.Value.Email.Should().Be("getme@ejemplo.com");
        result.Value.Nombre.Should().Be("Carlos");
        result.Value.Apellidos.Should().Be("López");
        result.Value.Role.Should().Be("Cliente");
    }

    /// <summary>
    /// Dado un ID de usuario que no existe, cuando se llama a GetMeAsync,
    /// entonces devuelve error.
    /// </summary>
    [Test]
    public async Task GetMeAsync_UsuarioNoExistente_DevuelveError()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await GetSut().GetMeAsync(userId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion
}

