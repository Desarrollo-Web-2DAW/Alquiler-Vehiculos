using System.Text;
using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios;
using Alquiler_Vehiculos.Services.Usuarios.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
/// Configuración de ASP.NET Core Identity, JWT Bearer y políticas de autorización (RF-46).
/// </summary>
public static class IdentityConfig {
    /// <summary>
    /// Registra Identity, JWT Bearer, políticas de autorización y <see cref="IAuthService"/>.
    /// </summary>
    public static IServiceCollection AddIdentityConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment) {
        Log.Information("Configurando Identity y JWT...");

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>() ?? new JwtSettings();

        if (string.IsNullOrWhiteSpace(jwtSettings.Key) || Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32) {
            throw new InvalidOperationException(
                "Falta 'Jwt:Key' o tiene menos de 32 bytes (256 bits para HMAC-SHA256). " +
                (environment.IsDevelopment()
                    ? "Configúrala con dotnet user-secrets."
                    : "Inyéctala mediante la variable de entorno Jwt__Key."));
        }

        services.AddIdentityCore<User>(options => {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;

                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddSignInManager()
            .AddEntityFrameworkStores<AppDbContextUsuarios>()
            .AddDefaultTokenProviders();

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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy(PolicyNames.Admin, new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireRole(nameof(RolUsuario.Admin))
                .Build())
            .AddPolicy(PolicyNames.Responsable, new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireRole(nameof(RolUsuario.Responsable), nameof(RolUsuario.Admin))
                .Build())
            .AddPolicy(PolicyNames.Trabajador, new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireRole(nameof(RolUsuario.Trabajador), nameof(RolUsuario.Responsable), nameof(RolUsuario.Admin))
                .Build())
            .AddPolicy(PolicyNames.Cliente, new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireRole(nameof(RolUsuario.Cliente), nameof(RolUsuario.Trabajador),
                    nameof(RolUsuario.Responsable), nameof(RolUsuario.Admin))
                .Build())
            .AddPolicy(PolicyNames.Mecanico, new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireRole(nameof(RolUsuario.Trabajador), nameof(RolUsuario.Responsable), nameof(RolUsuario.Admin))
                .RequireClaim("tipo_trabajador", nameof(TipoTrabajador.Mecanico))
                .Build());

        services.AddScoped<IAuthService, AuthService>();

        Log.Information("Identity y JWT configurados correctamente.");
        return services;
    }
}
