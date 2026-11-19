using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios.Clientes;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using Microsoft.AspNetCore.Identity;

namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
/// Siembra los roles de Identity y cuentas de prueba.
/// Es idempotente: si ya existen, no hace nada.
/// Las credenciales se leen de la sección <c>Seed</c> y solo deben estar
/// en <c>appsettings.Development.json</c> o en User Secrets.
/// </summary>
public static class IdentitySeeder {
    /// <summary>
    /// Crea los roles de <see cref="RolUsuario"/> y las cuentas configuradas.
    /// </summary>
    /// <param name="serviceProvider">Proveedor de servicios de la aplicación.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration) {
        using var scope = serviceProvider.CreateScope();

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("IdentitySeeder");

        await SeedRolesAsync(scope.ServiceProvider, logger);
        await SeedAdminUserAsync(scope.ServiceProvider, configuration, logger);
        await SeedClienteUserAsync(scope.ServiceProvider, configuration, logger);
        await SeedTrabajadorUserAsync(scope.ServiceProvider, configuration, logger);
    }

    /// <summary>Crea los roles del enum <see cref="RolUsuario"/> que falten.</summary>
    private static async Task SeedRolesAsync(IServiceProvider serviceProvider, ILogger logger) {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var role in Enum.GetNames<RolUsuario>()) {
            if (await roleManager.RoleExistsAsync(role)) continue;

            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            if (!result.Succeeded) {
                LogErrores(logger, $"crear el rol {role}", result.Errors);
                continue;
            }

            logger.LogInformation("Rol creado: {Role}", role);
        }
    }

    /// <summary>
    /// Crea el administrador. No se le asocia persona: solo administra.
    /// </summary>
    private static async Task SeedAdminUserAsync(
        IServiceProvider serviceProvider, IConfiguration configuration, ILogger logger) {
        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) {
            logger.LogWarning("Seed de admin omitido: faltan Seed:AdminEmail o Seed:AdminPassword");
            return;
        }

        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        await EnsureUserAsync(
            userManager, logger, email, password, nameof(RolUsuario.Admin));
    }

    /// <summary>
    /// Crea un cliente de prueba con cuenta. El cliente y la cuenta se insertan
    /// juntos porque <c>Cliente</c> va como navegación de <c>User</c>.
    /// </summary>
    private static async Task SeedClienteUserAsync(
        IServiceProvider serviceProvider, IConfiguration configuration, ILogger logger) {
        var email = configuration["Seed:ClienteEmail"];
        var password = configuration["Seed:ClientePassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) {
            return;
        }

        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        if (await userManager.FindByEmailAsync(email) is not null) return;

        var cliente = new Cliente {
            Nombre = "Cliente",
            Apellidos = "De Prueba",
            Email = email.Trim().ToLowerInvariant(),
            Telefono = "600000000",
            FechaNacimiento = new DateOnly(1995, 1, 1),
            Dni = "00000000T"
        };

        var user = new User {
            UserName = cliente.Email,
            Email = cliente.Email,
            EmailConfirmed = true,
            ClienteId = cliente.Id,
            Cliente = cliente
        };

        var creado = await userManager.CreateAsync(user, password);
        if (!creado.Succeeded) {
            LogErrores(logger, $"crear el cliente {email}", creado.Errors);
            return;
        }

        var asignado = await userManager.AddToRoleAsync(user, nameof(RolUsuario.Cliente));
        if (!asignado.Succeeded) {
            LogErrores(logger, $"asignar el rol Cliente a {email}", asignado.Errors);
            await userManager.DeleteAsync(user);
            return;
        }

        logger.LogInformation("Cliente sembrado: {Email}", email);
    }

    /// <summary>Crea un trabajador de prueba con cuenta y rol Trabajador.</summary>
    private static async Task SeedTrabajadorUserAsync(
        IServiceProvider serviceProvider, IConfiguration configuration, ILogger logger) {
        var email = configuration["Seed:TrabajadorEmail"];
        var password = configuration["Seed:TrabajadorPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) {
            return;
        }

        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        if (await userManager.FindByEmailAsync(email) is not null) return;

        var trabajador = new Trabajador {
            Nombre = "Trabajador",
            Apellidos = "De Prueba",
            Email = email.Trim().ToLowerInvariant(),
            Telefono = "611111111",
            FechaNacimiento = new DateOnly(1992, 6, 1),
            Dni = "11111111H",
            FechaIngreso = new DateOnly(2024, 2, 1),
            Tipo = TipoTrabajador.Operaciones
        };

        var user = new User {
            UserName = trabajador.Email,
            Email = trabajador.Email,
            EmailConfirmed = true,
            TrabajadorId = trabajador.Id,
            Trabajador = trabajador
        };

        var creado = await userManager.CreateAsync(user, password);
        if (!creado.Succeeded) {
            LogErrores(logger, $"crear el trabajador {email}", creado.Errors);
            return;
        }

        var asignado = await userManager.AddToRoleAsync(user, nameof(RolUsuario.Trabajador));
        if (!asignado.Succeeded) {
            LogErrores(logger, $"asignar el rol Trabajador a {email}", asignado.Errors);
            await userManager.DeleteAsync(user);
            return;
        }

        logger.LogInformation("Trabajador sembrado: {Email}", email);
    }

    /// <summary>
    /// Crea una cuenta con un rol si no existe. Se usa para cuentas sin persona
    /// (admin). Para personas hay que insertar además el cliente o el trabajador.
    /// </summary>
    private static async Task EnsureUserAsync(
        UserManager<User> userManager, ILogger logger,
        string email, string password, string role) {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null) {
            user = new User {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var creado = await userManager.CreateAsync(user, password);
            if (!creado.Succeeded) {
                LogErrores(logger, $"crear la cuenta {email}", creado.Errors);
                return;
            }

            logger.LogInformation("Cuenta sembrada: {Email}", email);
        }

        if (await userManager.IsInRoleAsync(user, role)) return;

        var asignado = await userManager.AddToRoleAsync(user, role);
        if (!asignado.Succeeded) {
            LogErrores(logger, $"asignar el rol {role} a {email}", asignado.Errors);
            return;
        }

        logger.LogInformation("Rol {Role} asignado a {Email}", role, email);
    }

    /// <summary>Escribe en el log los errores de Identity de forma legible.</summary>
    private static void LogErrores(ILogger logger, string contexto, IEnumerable<IdentityError> errores) {
        foreach (var error in errores) {
            logger.LogError("Error al {Contexto}: {Codigo} - {Descripcion}",
                contexto, error.Code, error.Description);
        }
    }
}
