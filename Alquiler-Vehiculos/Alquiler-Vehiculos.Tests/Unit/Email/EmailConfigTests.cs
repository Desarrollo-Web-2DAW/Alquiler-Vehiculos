using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Services.Email;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Alquiler_Vehiculos.Tests.Unit.Email;

/// <summary>
/// Tests unitarios del registro del módulo de correo en el contenedor (EmailConfig).
/// Requisito cubierto: RF-36 (según la configuración se envía por SMTP o con el
/// servicio simulado) y cableado de todo el módulo con una línea en Program.cs.
/// </summary>
[Category("Unit")]
public class EmailConfigTests {
    private static ServiceProvider CrearProvider(IDictionary<string, string?> valores) {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(valores)
            .Build();
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddSingleton<IHostEnvironment>(FakeHostEnvironment.ParaPlantillas());
        servicios.AddEmailNotifications(config);
        return servicios.BuildServiceProvider();
    }

    /// <summary>
    /// Dado que la configuración pide envío real, cuando se registran los servicios,
    /// entonces el envío es con MailKit y todo el módulo queda resoluble, con su worker.
    /// </summary>
    [Test]
    public void AddEmailNotifications_ConSmtp_RegistraElEnvioRealYTodoElModulo() {
        // Arrange
        using var provider = CrearProvider(new Dictionary<string, string?> {
            ["Email:UseSmtp"] = "true",
            ["Email:Host"] = "localhost",
            ["Email:Port"] = "1025"
        });

        // Act & Assert
        provider.GetRequiredService<IEmailService>().Should().BeOfType<MailKitEmailService>();
        provider.GetRequiredService<IEmailNotifier>().Should().BeOfType<EmailNotifier>();
        provider.GetRequiredService<IEmailTemplateRenderer>().Should().BeOfType<EmailTemplateRenderer>();
        provider.GetRequiredService<IEmailOutboxStore>().Should().BeOfType<InMemoryEmailOutboxStore>();
        provider.GetServices<IHostedService>().Should().Contain(s => s is EmailBackgroundWorker);
    }

    /// <summary>
    /// Dado que la configuración desactiva el SMTP, cuando se registran los servicios,
    /// entonces el envío usa la implementación simulada en memoria.
    /// </summary>
    [Test]
    public void AddEmailNotifications_SinSmtp_RegistraElServicioSimulado() {
        // Arrange
        using var provider = CrearProvider(new Dictionary<string, string?> {
            ["Email:UseSmtp"] = "false"
        });

        // Act & Assert
        provider.GetRequiredService<IEmailService>().Should().BeOfType<MemoryEmailService>();
    }

    /// <summary>
    /// Dado que no hay sección de configuración, cuando se registran los servicios,
    /// entonces se usan los valores por defecto (envío real) sin fallar.
    /// </summary>
    [Test]
    public void AddEmailNotifications_SinSeccion_UsaLosValoresPorDefecto() {
        // Arrange
        using var provider = CrearProvider(new Dictionary<string, string?>());

        // Act
        provider.GetRequiredService<IEmailService>().Should().BeOfType<MailKitEmailService>();
        var settings = provider.GetRequiredService<IOptions<EmailSettings>>().Value;

        // Assert
        settings.Host.Should().Be("localhost");
        settings.Port.Should().Be(1025);
        settings.Username.Should().BeNull();
        settings.Password.Should().BeNull();
        settings.From.Should().Be("no-reply@alquilervehiculos.local");
        settings.FromName.Should().Be("Alquiler Vehículos");
        settings.Security.Should().Be("None");
        settings.UseSmtp.Should().BeTrue();
        settings.MaxIntentos.Should().Be(5);
        settings.IntervaloBaseReintentosSegundos.Should().Be(30);
        settings.FrecuenciaProcesoSegundos.Should().Be(10);
        settings.LoteMaximo.Should().Be(20);
    }
}
