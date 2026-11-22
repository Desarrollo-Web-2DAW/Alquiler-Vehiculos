using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios.Clientes;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using Microsoft.AspNetCore.Identity;

namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
/// Siembra los roles de Identity y las cuentas iniciales de todos los perfiles (RF-46, RI-03).
/// Es idempotente: si ya existen, no hace nada.
/// Las credenciales se leen de la sección <c>Seed</c> (appsettings.Development.json,
/// User Secrets o variables de entorno).
/// </summary>
public static class IdentitySeeder {
    /// <summary>
    /// Crea los roles de <see cref="RolUsuario"/> y las cuentas configuradas.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration) {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");

        await SeedRolesAsync(sp, logger);
        await SeedAdminUserAsync(sp, configuration, logger);
        await SeedClienteUserAsync(sp, configuration, logger);

        // Trabajador de operaciones (RF-12, RF-46)
        await SeedTrabajadorUserAsync(
            sp, configuration, logger,
            emailKey: "Seed:TrabajadorEmail",
            passwordKey: "Seed:TrabajadorPassword",
            nombre: "Carlos",
            apellidos: "Martín López",
            telefono: "611223344",
            dni: "23456789D",
            fechaNacimiento: new DateOnly(1992, 6, 1),
            fechaIngreso: new DateOnly(2024, 2, 1),
            tipo: TipoTrabajador.Operaciones,
            rol: nameof(RolUsuario.Trabajador));

        // Mecánico de taller (RF-08, RF-12, CU-07)
        await SeedTrabajadorUserAsync(
            sp, configuration, logger,
            emailKey: "Seed:MecanicoEmail",
            passwordKey: "Seed:MecanicoPassword",
            nombre: "Laura",
            apellidos: "Gómez Ruiz",
            telefono: "622334455",
            dni: "34567890V",
            fechaNacimiento: new DateOnly(1989, 11, 15),
            fechaIngreso: new DateOnly(2023, 5, 10),
            tipo: TipoTrabajador.Mecanico,
            rol: nameof(RolUsuario.Trabajador));

        // Responsable de la oficina (RF-11, RF-46, CU-09, CU-18)
        await SeedTrabajadorUserAsync(
            sp, configuration, logger,
            emailKey: "Seed:ResponsableEmail",
            passwordKey: "Seed:ResponsablePassword",
            nombre: "Elena",
            apellidos: "Sánchez Navarro",
            telefono: "633445566",
            dni: "45678901G",
            fechaNacimiento: new DateOnly(1985, 3, 20),
            fechaIngreso: new DateOnly(2022, 1, 15),
            tipo: TipoTrabajador.Operaciones,
            rol: nameof(RolUsuario.Responsable));
    }

    private static async Task SeedRolesAsync(IServiceProvider sp, ILogger logger) {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

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

    private static async Task SeedAdminUserAsync(
        IServiceProvider sp, IConfiguration configuration, ILogger logger) {
        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) {
            logger.LogWarning("Seed de admin omitido: faltan Seed:AdminEmail o Seed:AdminPassword");
            return;
        }

        var userManager = sp.GetRequiredService<UserManager<User>>();
        await EnsureUserAsync(userManager, logger, email.Trim().ToLowerInvariant(), password, nameof(RolUsuario.Admin));
    }

    private static async Task SeedClienteUserAsync(
        IServiceProvider sp, IConfiguration configuration, ILogger logger) {
        var email = configuration["Seed:ClienteEmail"]?.Trim().ToLowerInvariant();
        var password = configuration["Seed:ClientePassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        var userManager = sp.GetRequiredService<UserManager<User>>();
        if (await userManager.FindByEmailAsync(email) is not null) return;

        var db = sp.GetRequiredService<AppDbContextUsuarios>();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var cliente = new Cliente {
            Nombre = "Ana",
            Apellidos = "García Pérez",
            Email = email,
            Telefono = "600112233",
            FechaNacimiento = new DateOnly(1995, 4, 12),
            Dni = "12345678Z"
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
            await transaction.RollbackAsync();
            LogErrores(logger, "crear el cliente semilla", creado.Errors);
            return;
        }

        var asignado = await userManager.AddToRoleAsync(user, nameof(RolUsuario.Cliente));
        if (!asignado.Succeeded) {
            await transaction.RollbackAsync();
            LogErrores(logger, "asignar el rol Cliente", asignado.Errors);
            return;
        }

        await transaction.CommitAsync();
        logger.LogInformation("Cliente semilla creado: {ClienteId}", cliente.Id);
    }

    private static async Task SeedTrabajadorUserAsync(
        IServiceProvider sp,
        IConfiguration configuration,
        ILogger logger,
        string emailKey,
        string passwordKey,
        string nombre,
        string apellidos,
        string telefono,
        string dni,
        DateOnly fechaNacimiento,
        DateOnly fechaIngreso,
        TipoTrabajador tipo,
        string rol) {
        var email = configuration[emailKey]?.Trim().ToLowerInvariant();
        var password = configuration[passwordKey];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        var userManager = sp.GetRequiredService<UserManager<User>>();
        if (await userManager.FindByEmailAsync(email) is not null) return;

        var db = sp.GetRequiredService<AppDbContextUsuarios>();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var trabajador = new Trabajador {
            Nombre = nombre,
            Apellidos = apellidos,
            Email = email,
            Telefono = telefono,
            FechaNacimiento = fechaNacimiento,
            Dni = dni,
            FechaIngreso = fechaIngreso,
            Tipo = tipo
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
            await transaction.RollbackAsync();
            LogErrores(logger, $"crear trabajador semilla ({rol})", creado.Errors);
            return;
        }

        var asignado = await userManager.AddToRoleAsync(user, rol);
        if (!asignado.Succeeded) {
            await transaction.RollbackAsync();
            LogErrores(logger, $"asignar rol {rol}", asignado.Errors);
            return;
        }

        await transaction.CommitAsync();
        logger.LogInformation("Trabajador semilla creado: {TrabajadorId} ({Rol}/{Tipo})",
            trabajador.Id, rol, tipo);
    }

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
                LogErrores(logger, $"crear la cuenta ({role})", creado.Errors);
                return;
            }

            logger.LogInformation("Cuenta semilla creada: {UserId} ({Role})", user.Id, role);
        }

        if (await userManager.IsInRoleAsync(user, role)) return;

        var asignado = await userManager.AddToRoleAsync(user, role);
        if (!asignado.Succeeded) {
            LogErrores(logger, $"asignar el rol {role}", asignado.Errors);
            return;
        }

        logger.LogInformation("Rol {Role} asignado a {UserId}", role, user.Id);
    }

    private static void LogErrores(ILogger logger, string contexto, IEnumerable<IdentityError> errores) {
        foreach (var error in errores) {
            logger.LogError("Error al {Contexto}: {Codigo} - {Descripcion}",
                contexto, error.Code, error.Description);
        }
    }
}
