using System.Net.Http.Json;
using Alquiler_Vehiculos.Dto.Email;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Models.Email;
using Alquiler_Vehiculos.Services.Email;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;

namespace Alquiler_Vehiculos.Tests.Integration.Email;

/// <summary>
/// Tests de integración del envío de correo contra Mailpit (SMTP real en contenedor),
/// con Testcontainers (RNF-04 y RF-36). Comprueban el camino real de envío (sin
/// cliente SMTP simulado) y el flujo completo notificador → cola → worker.
/// Requiere Docker: si no está disponible, el arranque del contenedor falla
/// y el bloqueo queda a la vista.
/// </summary>
[Category("Integration")]
public class EmailSmtpIntegrationTests {
    private static readonly IContainer _container = new ContainerBuilder("axllent/mailpit:latest")
        .WithPortBinding(1025, true)
        .WithPortBinding(8025, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8025)))
        .Build();

    private HttpClient _api = null!;

    [OneTimeSetUp]
    public async Task ArrancarMailpit() {
        await _container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task PararMailpit() {
        await _container.DisposeAsync();
    }

    [SetUp]
    public void Setup() {
        _api = new HttpClient {
            BaseAddress = new Uri($"http://{_container.Hostname}:{_container.GetMappedPublicPort(8025)}")
        };
    }

    [TearDown]
    public void TearDown() {
        _api.Dispose();
    }

    private EmailSettings Settings() {
        return new EmailSettings {
            Host = _container.Hostname,
            Port = _container.GetMappedPublicPort(1025),
            Username = "",
            Password = "",
            From = "prueba@ejemplo.com",
            FromName = "Pruebas",
            Security = "None",
            UseSmtp = true
        };
    }

    private static EmailOutboxMessage Mensaje(string asunto) {
        return new EmailOutboxMessage {
            Tipo = TipoNotificacion.PagoAprobado,
            ClaveIdempotencia = "PagoAprobado:" + asunto,
            DestinatarioEmail = "cliente@ejemplo.com",
            DestinatarioNombre = "Cliente",
            Asunto = asunto,
            CuerpoHtml = "<p>Hola desde la integración</p>"
        };
    }

    /// <summary>
    /// Dado que hay un servidor SMTP real en un contenedor, cuando se envía
    /// con el servicio de MailKit sin cliente simulado, entonces el correo
    /// queda capturado en la bandeja del servidor.
    /// </summary>
    [Test]
    public async Task Enviar_MailkitContraMailpit_ElCorreoQuedaCapturado() {
        // Arrange
        var servicio = new MailKitEmailService(
            Options.Create(Settings()), NullLogger<MailKitEmailService>.Instance);
        var mensaje = Mensaje("Captura directa");

        // Act
        await servicio.EnviarAsync(mensaje);

        // Assert
        var bandeja = await _api.GetStringAsync("/api/v1/messages");
        bandeja.Should().Contain("cliente@ejemplo.com");
        bandeja.Should().Contain("Captura directa");
    }

    /// <summary>
    /// Dado que se notifica un evento con el módulo completo, cuando el worker
    /// procesa la cola contra el servidor real, entonces el cliente recibe
    /// exactamente un correo (flujo de extremo a extremo, CU-17).
    /// </summary>
    [Test]
    public async Task FlujoCompleto_NotificadorYWorker_ElClienteRecibeUnSoloCorreo() {
        // Arrange
        var outbox = new InMemoryEmailOutboxStore();
        var notificador = new EmailNotifier(
            new Services.Email.EmailTemplateRenderer(FakeHostEnvironmentParaPlantillas()),
            outbox,
            NullLogger<EmailNotifier>.Instance);
        var servicios = new ServiceCollection();
        servicios.AddSingleton<IEmailService>(new MailKitEmailService(
            Options.Create(Settings()), NullLogger<MailKitEmailService>.Instance));
        using var provider = servicios.BuildServiceProvider();
        var worker = new EmailBackgroundWorker(
            outbox, provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new EmailSettings { FrecuenciaProcesoSegundos = 1, IntervaloBaseReintentosSegundos = 1, MaxIntentos = 3, LoteMaximo = 10 }),
            NullLogger<EmailBackgroundWorker>.Instance);

        // Act
        await notificador.NotificarPagoAprobadoAsync(new PagoAprobadoEmail {
            CodigoReferencia = "REF-E2E", ClienteNombre = "Cliente", ClienteEmail = "cliente@ejemplo.com",
            Importe = 100m, MarcaTarjeta = "Visa", UltimosCuatroDigitos = "1234",
            FechaPago = DateTimeOffset.Now, FechaRecogida = DateTimeOffset.Now, VehiculoDescripcion = "Ford Transit"
        });
        await notificador.NotificarPagoAprobadoAsync(new PagoAprobadoEmail {
            CodigoReferencia = "REF-E2E", ClienteNombre = "Cliente", ClienteEmail = "cliente@ejemplo.com",
            Importe = 100m, MarcaTarjeta = "Visa", UltimosCuatroDigitos = "1234",
            FechaPago = DateTimeOffset.Now, FechaRecogida = DateTimeOffset.Now, VehiculoDescripcion = "Ford Transit"
        });
        await worker.StartAsync(CancellationToken.None);

        // Assert
        await EsperarAsync(async () => {
            var bandeja = await _api.GetFromJsonAsync<Bandeja>("/api/v1/messages");
            return bandeja is not null && bandeja.Total >= 1;
        }, "que el correo llegue a la bandeja");
        await worker.StopAsync(CancellationToken.None);

        var mensajes = await _api.GetFromJsonAsync<Bandeja>("/api/v1/messages");
        mensajes!.Messages.Count(m => m.Subject.Contains("REF-E2E")).Should().Be(1, "el mismo evento no debe duplicarse (CU-17)");
    }

    private static Microsoft.Extensions.Hosting.IHostEnvironment FakeHostEnvironmentParaPlantillas() {
        return Unit.Email.FakeHostEnvironment.ParaPlantillas();
    }

    private static async Task EsperarAsync(Func<Task<bool>> condicion, string motivo, int timeoutMs = 15000) {
        var limite = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < limite) {
            if (await condicion()) {
                return;
            }
            await Task.Delay(250);
        }
        Assert.Fail($"Tiempo agotado esperando: {motivo}");
    }

    private sealed record Bandeja(int Total, List<MensajeBandeja> Messages);
    private sealed record MensajeBandeja(string Subject);
}
