using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Models.Email;
using Alquiler_Vehiculos.Services.Email;
using FluentAssertions;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Email;

/// <summary>
/// Tests unitarios del envío SMTP con MailKit (MailKitEmailService) sobre un
/// cliente SMTP simulado, sin servidor real. Requisito cubierto: RF-35 (el
/// envío real se puede probar y sus fallos se propagan para que el worker
/// registre y reintente).
/// </summary>
[Category("Unit")]
public class MailKitEmailServiceTests {
    private Mock<ISmtpClient> _smtp = null!;
    private MimeMessage? _mensajeEnviado;

    [SetUp]
    public void Setup() {
        _mensajeEnviado = null;
        _smtp = new Mock<ISmtpClient>();
        _smtp.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _smtp.Setup(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _smtp.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
            .Callback<MimeMessage, CancellationToken, ITransferProgress?>((mensaje, _, _) => _mensajeEnviado = mensaje)
            .ReturnsAsync("250 OK");
        _smtp.Setup(c => c.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private MailKitEmailService CrearServicio(string security = "None", string? username = null) {
        var settings = new EmailSettings {
            Host = "smtp.ejemplo.com",
            Port = 587,
            Username = username,
            Password = "secreto",
            From = "no-reply@ejemplo.com",
            FromName = "Alquiler Vehículos",
            Security = security
        };
        return new MailKitEmailService(Options.Create(settings), NullLogger<MailKitEmailService>.Instance, _smtp.Object);
    }

    private static EmailOutboxMessage Mensaje() {
        return new EmailOutboxMessage {
            Tipo = TipoNotificacion.PagoAprobado,
            ClaveIdempotencia = "PagoAprobado:REF-001",
            DestinatarioEmail = "lucia@ejemplo.com",
            DestinatarioNombre = "Lucía",
            Asunto = "Pago aprobado",
            CuerpoHtml = "<p>Hola</p>"
        };
    }

    /// <summary>
    /// Dado que hay credenciales configuradas, cuando se envía,
    /// entonces se conecta, se autentica, se envía el mensaje y se desconecta.
    /// </summary>
    [Test]
    public async Task Enviar_ConCredenciales_ConectaAutenticaEnviaYDesconecta() {
        // Arrange
        var servicio = CrearServicio(username: "usuario");
        var mensaje = Mensaje();

        // Act
        await servicio.EnviarAsync(mensaje);

        // Assert
        _smtp.Verify(c => c.ConnectAsync("smtp.ejemplo.com", 587, It.IsAny<SecureSocketOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _smtp.Verify(c => c.AuthenticateAsync("usuario", "secreto", It.IsAny<CancellationToken>()), Times.Once);
        _smtp.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(),
            It.IsAny<ITransferProgress>()), Times.Once);
        _smtp.Verify(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
        _mensajeEnviado.Should().NotBeNull();
        _mensajeEnviado!.Subject.Should().Be("Pago aprobado");
        _mensajeEnviado.To.Mailboxes.Single().Address.Should().Be("lucia@ejemplo.com");
        _mensajeEnviado.From.Mailboxes.Single().Address.Should().Be("no-reply@ejemplo.com");
    }

    /// <summary>
    /// Dado que no hay usuario SMTP (servidor sin autenticación), cuando se envía,
    /// entonces se conecta y envía sin autenticarse.
    /// </summary>
    [Test]
    public async Task Enviar_SinCredenciales_NoSeAutentica() {
        // Arrange
        var servicio = CrearServicio(username: null);

        // Act
        await servicio.EnviarAsync(Mensaje());

        // Assert
        _smtp.Verify(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Dado que la configuración fija un modo de seguridad, cuando se envía,
    /// entonces se conecta con el SecureSocketOptions correspondiente.
    /// </summary>
    [TestCase("Ssl", SecureSocketOptions.SslOnConnect)]
    [TestCase("Tls", SecureSocketOptions.StartTls)]
    [TestCase("None", SecureSocketOptions.None)]
    [TestCase("Auto", SecureSocketOptions.Auto)]
    [TestCase("otro", SecureSocketOptions.Auto)]
    public async Task Enviar_SeguridadConfigurada_ConectaConElModoAdecuado(
        string security, SecureSocketOptions esperado) {
        // Arrange
        var servicio = CrearServicio(security);

        // Act
        await servicio.EnviarAsync(Mensaje());

        // Assert
        _smtp.Verify(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), esperado,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Dado que el servidor SMTP rechaza el envío, cuando se envía,
    /// entonces la excepción se propaga para que el worker la registre y reintente (RF-35).
    /// </summary>
    [Test]
    public async Task Enviar_FalloDeSmtp_PropagaLaExcepcion() {
        // Arrange
        _smtp.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(),
                It.IsAny<ITransferProgress>()))
            .ThrowsAsync(new InvalidOperationException("SMTP caído"));
        var servicio = CrearServicio();
        Func<Task> act = () => servicio.EnviarAsync(Mensaje());

        // Act & Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
