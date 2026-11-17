using System.Text;
using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
/// Configuración de Identity, JWT y autorización.
/// </summary>
public static class IdentityConfig {
    /// <summary>
    /// Registra Identity, JWT Bearer y políticas de autorización.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <param name="environment">Entorno de la aplicación.</param>
    /// <returns>IServiceCollection para encadenar.</returns>
    public static IServiceCollection AddIdentityConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment) {
        Log.Information("Configurando Identity y JWT...");

        // ── JWT Settings ────────────────────────────────────────────────────
        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));

        var jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>() ?? new JwtSettings();

        // ── Identity ────────────────────────────────────────────────────────
        // Configuración manual de Identity:
        // - contraseñas, bloqueo por intentos fallidos y email único
        // - roles, SignInManager y almacenamiento con EF Core
        services.AddIdentityCore<User>(options => {
                // Password
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                // Lockout
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;

                // User
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddSignInManager()
            .AddEntityFrameworkStores<AppDbContextUsuarios>();

        // ── JWT Authentication ──────────────────────────────────────────────
        // Se fuerza el esquema Bearer para que, en vez de redirigir a un login,
        // la API responda 401 o 403 en JSON.
        services.AddAuthentication(options => {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options => {
                options.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        // ── Authorization Policies ──────────────────────────────────────────
        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.Admin, policy => policy.RequireRole(
                nameof(RolUsuario.Admin)))
            .AddPolicy(PolicyNames.Cliente, policy => policy.RequireRole(
                nameof(RolUsuario.Cliente), nameof(RolUsuario.Admin)))
            .AddPolicy(PolicyNames.Trabajador, policy => policy.RequireRole(
                nameof(RolUsuario.Trabajador), nameof(RolUsuario.Responsable), nameof(RolUsuario.Admin)))
            .AddPolicy(PolicyNames.Responsable, policy => policy.RequireRole(
                nameof(RolUsuario.Responsable), nameof(RolUsuario.Admin)));

        Log.Information("Identity y JWT configurados correctamente.");
        return services;
    }
}
