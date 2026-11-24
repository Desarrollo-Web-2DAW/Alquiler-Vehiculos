using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Models.Usuarios;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Infrastructure;

[TestFixture]
[Category("Unit")]
public class IdentitySeederTests {
    private Mock<UserManager<User>> _users = null!;
    private Mock<RoleManager<IdentityRole<Guid>>> _roles = null!;
    private ServiceProvider _provider = null!;
    private readonly string _password = Guid.NewGuid() + "aA1!";

    [SetUp]
    public void SetUp() {
        _users = IdentityManagerMocks.Usuarios();
        _roles = IdentityManagerMocks.Roles();
        _roles.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_users.Object);
        services.AddSingleton(_roles.Object);
        // El seeder abre transacciones y siembra la oficina: InMemory basta y no persiste entre tests.
        var baseDatos = Guid.NewGuid().ToString();
        services.AddDbContext<AppDbContextUsuarios>(o => o
            .UseInMemoryDatabase(baseDatos)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        _provider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => _provider.Dispose();

    private static IConfiguration Configuracion(Dictionary<string, string?>? valores = null) =>
        new ConfigurationBuilder().AddInMemoryCollection(valores ?? new()).Build();

    private static IdentityResult Fallo() => IdentityResult.Failed(
        new IdentityError { Code = "ERROR_PRUEBA", Description = "Operacion rechazada" },
        new IdentityError { Code = "OTRO_ERROR", Description = "Segundo error" });

    [TestCase("existen")]
    [TestCase("creados")]
    [TestCase("fallan")]
    public async Task SeedAsync_RolesYCredencialesAusentes_NoCreaCuentas(string estado) {
        // Arrange
        _roles.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(estado == "existen");
        _roles.Setup(r => r.CreateAsync(It.IsAny<IdentityRole<Guid>>()))
            .ReturnsAsync(estado == "fallan" ? Fallo() : IdentityResult.Success);
        var configuration = Configuracion();

        // Act
        await IdentitySeeder.SeedAsync(_provider, configuration);

        // Assert
        foreach (var rol in Enum.GetNames<RolUsuario>()) {
            _roles.Verify(r => r.RoleExistsAsync(rol), Times.Once);
            _roles.Verify(r => r.CreateAsync(It.Is<IdentityRole<Guid>>(r => r.Name == rol)),
                estado == "existen" ? Times.Never() : Times.Once());
        }
        _users.Verify(u => u.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        _users.Verify(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        _users.Verify(u => u.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [TestCase("Admin", "creada")]
    [TestCase("Admin", "existe")]
    [TestCase("Admin", "creacionFalla")]
    [TestCase("Admin", "rolFalla")]
    [TestCase("Cliente", "creada")]
    [TestCase("Cliente", "existe")]
    [TestCase("Cliente", "creacionFalla")]
    [TestCase("Cliente", "rolFalla")]
    [TestCase("Trabajador", "creada")]
    [TestCase("Trabajador", "existe")]
    [TestCase("Trabajador", "creacionFalla")]
    [TestCase("Trabajador", "rolFalla")]
    public async Task SeedAsync_AltaDeCadaPerfil_RespetaErroresSinBorrarCuentas(string perfil, string estado) {
        // Arrange
        const string email = "persona@example.com";
        var existente = new User { Email = email };
        var configuration = Configuracion(new() { [$"Seed:{perfil}Email"] = email, [$"Seed:{perfil}Password"] = _password });
        _users.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(estado == "existe" ? existente : null);
        _users.Setup(u => u.IsInRoleAsync(It.IsAny<User>(), perfil)).ReturnsAsync(estado == "existe");
        User? creada = null;
        _users.Setup(u => u.CreateAsync(It.IsAny<User>(), _password))
            .Callback<User, string>((u, _) => creada = u)
            .ReturnsAsync(estado == "creacionFalla" ? Fallo() : IdentityResult.Success);
        _users.Setup(u => u.AddToRoleAsync(It.IsAny<User>(), perfil))
            .ReturnsAsync(estado == "rolFalla" ? Fallo() : IdentityResult.Success);
        _users.Setup(u => u.DeleteAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);

        // Act
        await IdentitySeeder.SeedAsync(_provider, configuration);

        // Assert
        _users.Verify(u => u.FindByEmailAsync(email), Times.Once);
        _users.Verify(u => u.CreateAsync(It.IsAny<User>(), _password), estado == "existe" ? Times.Never() : Times.Once());
        _users.Verify(u => u.AddToRoleAsync(It.IsAny<User>(), perfil), estado is "creada" or "rolFalla" ? Times.Once() : Times.Never());
        // Si falla el rol, Cliente y Trabajador deshacen el alta con la transacción, no borrando la cuenta.
        _users.Verify(u => u.DeleteAsync(It.IsAny<User>()), Times.Never);
        _users.Verify(u => u.IsInRoleAsync(It.IsAny<User>(), perfil), perfil == "Admin" && estado != "creacionFalla" ? Times.Once() : Times.Never());
        _roles.Verify(r => r.RoleExistsAsync(It.IsAny<string>()), Times.Exactly(4));
        if (estado != "existe") {
            creada.Should().NotBeNull();
            creada!.Email.Should().Be(email);
            creada.UserName.Should().Be(email);
            creada.EmailConfirmed.Should().BeTrue();
            if (perfil == "Cliente") {
                creada.Cliente.Should().NotBeNull();
                creada.ClienteId.Should().Be(creada.Cliente!.Id);
                creada.Cliente.Nombre.Should().Be("Ana");
                creada.Cliente.Dni.Should().Be("12345678Z");
            } else if (perfil == "Trabajador") {
                creada.Trabajador.Should().NotBeNull();
                creada.TrabajadorId.Should().Be(creada.Trabajador!.Id);
                creada.Trabajador.Nombre.Should().Be("Carlos");
                creada.Trabajador.Dni.Should().Be("23456789D");
            } else {
                creada.Cliente.Should().BeNull();
                creada.Trabajador.Should().BeNull();
            }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SeedAsync_AdminExistenteSinRol_AsignaRolSinRecrearCuenta(bool falla) {
        // Arrange
        const string email = "admin@example.com";
        var admin = new User { Email = email };
        _users.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(admin);
        _users.Setup(u => u.IsInRoleAsync(admin, "Admin")).ReturnsAsync(false);
        _users.Setup(u => u.AddToRoleAsync(admin, "Admin")).ReturnsAsync(falla ? Fallo() : IdentityResult.Success);
        var configuration = Configuracion(new() { ["Seed:AdminEmail"] = email, ["Seed:AdminPassword"] = _password });

        // Act
        await IdentitySeeder.SeedAsync(_provider, configuration);

        // Assert
        _users.Verify(u => u.FindByEmailAsync(email), Times.Once);
        _users.Verify(u => u.IsInRoleAsync(admin, "Admin"), Times.Once);
        _users.Verify(u => u.AddToRoleAsync(admin, "Admin"), Times.Once);
        _users.Verify(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        _users.Verify(u => u.DeleteAsync(It.IsAny<User>()), Times.Never);
        _roles.Verify(r => r.RoleExistsAsync(It.IsAny<string>()), Times.Exactly(4));
    }

    [TestCase("Admin")]
    [TestCase("Cliente")]
    [TestCase("Trabajador")]
    public async Task SeedAsync_EmailSinPassword_NoBuscaNiCreaCuenta(string perfil) {
        // Arrange
        var configuration = Configuracion(new() { [$"Seed:{perfil}Email"] = "persona@example.com", [$"Seed:{perfil}Password"] = " " });

        // Act
        await IdentitySeeder.SeedAsync(_provider, configuration);

        // Assert
        _users.Verify(u => u.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        _users.Verify(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        _roles.Verify(r => r.RoleExistsAsync(It.IsAny<string>()), Times.Exactly(4));
    }
}
