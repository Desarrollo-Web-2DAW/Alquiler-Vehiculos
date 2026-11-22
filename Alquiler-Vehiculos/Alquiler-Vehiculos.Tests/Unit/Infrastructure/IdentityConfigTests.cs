using System.Security.Claims;
using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Alquiler_Vehiculos.Tests.Unit.Infrastructure;

[TestFixture]
[Category("Unit")]
public class IdentityConfigTests {
    [TestCase(false)]
    [TestCase(true)]
    public void AddIdentityConfiguration_ConYSinSeccion_ConfiguraIdentity(bool conSeccion) {
        // Arrange
        var valores = conSeccion ? new Dictionary<string, string?> {
            ["Jwt:Key"] = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            ["Jwt:Issuer"] = "issuer-tests", ["Jwt:Audience"] = "audience-tests", ["Jwt:ExpirationMinutes"] = "15"
        } : new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        var entorno = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" }).Environment;

        // Act
        var resultado = services.AddIdentityConfiguration(configuration, entorno);

        // Assert
        resultado.Should().BeSameAs(services);
        using var provider = services.BuildServiceProvider();
        var identity = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;
        identity.Password.RequiredLength.Should().Be(8);
        identity.Password.RequireDigit.Should().BeTrue();
        identity.Password.RequireLowercase.Should().BeTrue();
        identity.Password.RequireUppercase.Should().BeTrue();
        identity.Password.RequireNonAlphanumeric.Should().BeTrue();
        identity.User.RequireUniqueEmail.Should().BeTrue();
        identity.Lockout.MaxFailedAccessAttempts.Should().Be(5);
        identity.Lockout.DefaultLockoutTimeSpan.Should().Be(TimeSpan.FromMinutes(5));
        var auth = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        auth.DefaultScheme.Should().Be("Bearer");
        auth.DefaultAuthenticateScheme.Should().Be("Bearer");
        auth.DefaultChallengeScheme.Should().Be("Bearer");
        auth.DefaultForbidScheme.Should().Be("Bearer");
        var jwt = provider.GetRequiredService<IOptions<JwtSettings>>().Value;
        jwt.ExpirationMinutes.Should().Be(conSeccion ? 15 : 30);
        if (conSeccion) {
            var bearer = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("Bearer");
            var validation = bearer.TokenValidationParameters;
            validation.ValidateIssuer.Should().BeTrue();
            validation.ValidateAudience.Should().BeTrue();
            validation.ValidateLifetime.Should().BeTrue();
            validation.ValidateIssuerSigningKey.Should().BeTrue();
            validation.ValidIssuer.Should().Be("issuer-tests");
            validation.ValidAudience.Should().Be("audience-tests");
            validation.ClockSkew.Should().Be(TimeSpan.Zero);
            validation.IssuerSigningKey.Should().NotBeNull();
        }
    }

    [TestCase(PolicyNames.Admin, "Admin", true)]
    [TestCase(PolicyNames.Admin, "Responsable", false)]
    [TestCase(PolicyNames.Admin, "Trabajador", false)]
    [TestCase(PolicyNames.Admin, "Cliente", false)]
    [TestCase(PolicyNames.Responsable, "Admin", true)]
    [TestCase(PolicyNames.Responsable, "Responsable", true)]
    [TestCase(PolicyNames.Responsable, "Trabajador", false)]
    [TestCase(PolicyNames.Trabajador, "Admin", true)]
    [TestCase(PolicyNames.Trabajador, "Responsable", true)]
    [TestCase(PolicyNames.Trabajador, "Trabajador", true)]
    [TestCase(PolicyNames.Trabajador, "Cliente", false)]
    [TestCase(PolicyNames.Cliente, "Admin", true)]
    [TestCase(PolicyNames.Cliente, "Responsable", true)]
    [TestCase(PolicyNames.Cliente, "Trabajador", true)]
    [TestCase(PolicyNames.Cliente, "Cliente", true)]
    [TestCase(PolicyNames.Cliente, "Invitado", false)]
    public async Task Politica_RolDelUsuario_PermiteSoloRolesConfigurados(string politica, string rol, bool permitido) {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var entorno = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" }).Environment;
        services.AddIdentityConfiguration(new ConfigurationBuilder().Build(), entorno);
        using var provider = services.BuildServiceProvider();
        var usuario = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, rol)], "Testing"));
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        // Act
        var resultado = await authorization.AuthorizeAsync(usuario, null, politica);

        // Assert
        resultado.Succeeded.Should().Be(permitido);
    }
}
