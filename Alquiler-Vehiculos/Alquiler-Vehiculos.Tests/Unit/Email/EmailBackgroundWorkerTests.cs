using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Models.Email;
using Alquiler_Vehiculos.Services.Email;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Alquiler_Vehiculos.Tests.Unit.Email;

/// <summary>
/// Tests unitarios del worker de envío de correos (EmailBackgroundWorker).
/// Requisitos cubiertos: RF-35 (el fallo de correo queda registrado y se
/// reintenta sin romper nada) y CU-17 (cada evento se entrega una sola vez).
/// </summary>
[Category("Unit")]
public class EmailBackgroundWorkerTests {
    private InMemoryEmailOutboxStore _outbox = null!;
    private FakeEmailService _sender = null!;
    private ServiceProvider _provider = null!;

    [SetUp]
    public void Setup() {
        _outbox = new InMemoryEmailOutboxStore();
        _sender = new FakeEmailService();
        var servicios = new ServiceCollection();
        servicios.AddSingleton<IEmailService>(_sender);
        _provider = servicios.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() {
        _provider.Dispose();
    }

    private EmailBackgroundWorker CrearWorker(EmailSettings? settings = null, IEmailOutboxStore? outbox = null) {
        settings ??= new EmailSettings {
            FrecuenciaProcesoSegundos = 1,
            IntervaloBaseReintentosSegundos = 1,
            MaxIntentos = 3,
            LoteMaximo = 10
        };
        return new EmailBackgroundWorker(
            outbox ?? _outbox,
            _provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(settings),
            NullLogger<EmailBackgroundWorker>.Instance);
    }

    private static EmailOutboxMessage Mensaje(string clave = "ReservaCreada:REF-001") {
        return new EmailOutboxMessage {
            Tipo = TipoNotificacion.ReservaCreada,
            ClaveIdempotencia = clave,
            DestinatarioEmail = "lucia@ejemplo.com",
            DestinatarioNombre = "Lucía",
            Asunto = "Asunto",
            CuerpoHtml = "<p>Hola</p>"
        };
    }

    private static async Task EsperarAsync(Func<bool> condicion, string motivo, int timeoutMs = 10000) {
        var limite = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < limite) {
            if (condicion()) {
                return;
            }
            await Task.Delay(50);
        }
        Assert.Fail($"Tiempo agotado esperando: {motivo}");
    }

    /// <summary>
    /// Dado que hay un mensaje pendiente, cuando el worker procesa la cola,
    /// entonces lo envía y lo marca como enviado con su fecha (RF-35).
    /// </summary>
    [Test]
    public async Task Procesar_MensajePendiente_EnviaYMarcaEnviado() {
        // Arrange
        var mensaje = Mensaje();
        await _outbox.AgregarAsync(mensaje);
        var worker = CrearWorker();

        // Act
        await worker.StartAsync(CancellationToken.None);
        await EsperarAsync(() => mensaje.Estado == EstadoEmailOutbox.Enviado, "el mensaje se marque como enviado");
        await worker.StopAsync(CancellationToken.None);

        // Assert
        _sender.Enviados.Should().HaveCount(1);
        mensaje.FechaEnvio.Should().NotBeNull();
        mensaje.UltimoError.Should().BeNull();
    }

    /// <summary>
    /// Dado que el servidor de correo falla, cuando se agotan los intentos,
    /// entonces el mensaje queda como Fallido con el error registrado para revisión (RF-35).
    /// </summary>
    [Test]
    public async Task Procesar_EnvioQueFalla_ReintentaYMarcaFallido() {
        // Arrange
        var mensaje = Mensaje();
        await _outbox.AgregarAsync(mensaje);
        var worker = CrearWorker(new EmailSettings {
            FrecuenciaProcesoSegundos = 1,
            IntervaloBaseReintentosSegundos = 1,
            MaxIntentos = 2,
            LoteMaximo = 10
        });
        _sender.FallosRestantes = int.MaxValue;

        // Act
        await worker.StartAsync(CancellationToken.None);
        await EsperarAsync(() => mensaje.Estado == EstadoEmailOutbox.Fallido, "el mensaje se marque como fallido");
        await worker.StopAsync(CancellationToken.None);

        // Assert
        mensaje.Intentos.Should().Be(2);
        mensaje.UltimoError.Should().Contain("SMTP caído");
        _sender.Enviados.Should().BeEmpty();
    }

    /// <summary>
    /// Dado que el envío falla una vez y luego funciona, cuando el worker reintenta,
    /// entonces el mensaje termina enviado con su reintento registrado (CU-17).
    /// </summary>
    [Test]
    public async Task Procesar_FalloTransitorio_ReintentaYTerminaEnviado() {
        // Arrange
        var mensaje = Mensaje();
        await _outbox.AgregarAsync(mensaje);
        var worker = CrearWorker(new EmailSettings {
            FrecuenciaProcesoSegundos = 1,
            IntervaloBaseReintentosSegundos = 1,
            MaxIntentos = 3,
            LoteMaximo = 10
        });
        _sender.FallosRestantes = 1;

        // Act
        await worker.StartAsync(CancellationToken.None);
        await EsperarAsync(() => mensaje.Estado == EstadoEmailOutbox.Enviado, "el mensaje se envíe tras el reintento");
        await worker.StopAsync(CancellationToken.None);

        // Assert
        mensaje.Intentos.Should().Be(1);
        _sender.Enviados.Should().HaveCount(1);
    }

    /// <summary>
    /// Dado que la cola lanza un error inesperado, cuando el worker procesa,
    /// entonces el error se registra y el bucle sigue vivo procesando mensajes.
    /// </summary>
    [Test]
    public async Task Procesar_FalloDeLaCola_ElBucleSigueVivo() {
        // Arrange
        var mensaje = Mensaje();
        await _outbox.AgregarAsync(mensaje);
        var outboxQueFalla = new OutboxQueFallaUnaVez(_outbox);
        var worker = CrearWorker(outbox: outboxQueFalla);

        // Act
        await worker.StartAsync(CancellationToken.None);
        await EsperarAsync(() => mensaje.Estado == EstadoEmailOutbox.Enviado, "el worker se recupere y envíe");
        await worker.StopAsync(CancellationToken.None);

        // Assert
        outboxQueFalla.Fallos.Should().Be(1);
    }

    /// <summary>
    /// Dado que la cola se cancela durante el procesado, cuando el worker la lanza,
    /// entonces termina su bucle sin error (apagado normal).
    /// </summary>
    [Test]
    public async Task Procesar_ColadeCancelada_TerminaElBucleSinError() {
        // Arrange
        var outboxCancelado = new OutboxQueCancela();
        var worker = CrearWorker(outbox: outboxCancelado);

        // Act
        await worker.StartAsync(CancellationToken.None);
        await EsperarAsync(() => outboxCancelado.Llamadas >= 1, "que el worker consulte la cola");
        await worker.StopAsync(CancellationToken.None);

        // Assert
        outboxCancelado.Llamadas.Should().Be(1);
    }

    private sealed class FakeEmailService : IEmailService {
        public List<EmailOutboxMessage> Enviados { get; } = [];
        public int FallosRestantes { get; set; }

        public Task EnviarAsync(EmailOutboxMessage mensaje, CancellationToken cancellationToken = default) {
            if (FallosRestantes > 0) {
                FallosRestantes--;
                throw new InvalidOperationException("SMTP caído");
            }
            Enviados.Add(mensaje);
            return Task.CompletedTask;
        }
    }

    private sealed class OutboxQueFallaUnaVez(IEmailOutboxStore interno) : IEmailOutboxStore {
        public int Fallos { get; private set; }

        public Task<bool> AgregarAsync(EmailOutboxMessage mensaje, CancellationToken cancellationToken = default)
            => interno.AgregarAsync(mensaje, cancellationToken);

        public Task<IReadOnlyList<EmailOutboxMessage>> ObtenerPendientesAsync(int lote, CancellationToken cancellationToken = default) {
            if (Fallos == 0) {
                Fallos++;
                throw new InvalidOperationException("fallo inesperado de la cola");
            }
            return interno.ObtenerPendientesAsync(lote, cancellationToken);
        }

        public Task ActualizarAsync(EmailOutboxMessage mensaje, CancellationToken cancellationToken = default)
            => interno.ActualizarAsync(mensaje, cancellationToken);
    }

    private sealed class OutboxQueCancela : IEmailOutboxStore {
        public int Llamadas { get; private set; }

        public Task<bool> AgregarAsync(EmailOutboxMessage mensaje, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<IReadOnlyList<EmailOutboxMessage>> ObtenerPendientesAsync(int lote, CancellationToken cancellationToken = default) {
            Llamadas++;
            throw new OperationCanceledException("procesado cancelado");
        }

        public Task ActualizarAsync(EmailOutboxMessage mensaje, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
