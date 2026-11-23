using Alquiler_Vehiculos.Models.Email;
using Alquiler_Vehiculos.Services.Email;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Alquiler_Vehiculos.Tests.Unit.Email;

/// <summary>
/// Tests unitarios del servicio de correo simulado (MemoryEmailService),
/// el que se usa en pruebas con <c>Email:UseSmtp = false</c> (RF-36).
/// </summary>
[Category("Unit")]
public class MemoryEmailServiceTests {
    private MemoryEmailService _servicio = null!;

    [SetUp]
    public void Setup() {
        MemoryEmailService.Vaciar();
        _servicio = new MemoryEmailService(NullLogger<MemoryEmailService>.Instance);
    }

    [TearDown]
    public void TearDown() {
        MemoryEmailService.Vaciar();
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
    /// Dado que se envía un correo simulado, cuando se envía,
    /// entonces queda registrado en memoria para poder inspeccionarlo en pruebas.
    /// </summary>
    [Test]
    public async Task Enviar_MensajeValido_LoRegistraEnMemoria() {
        // Arrange
        var mensaje = Mensaje();

        // Act
        await _servicio.EnviarAsync(mensaje);

        // Assert
        MemoryEmailService.Enviados.Should().HaveCount(1);
        MemoryEmailService.Enviados[0].Asunto.Should().Be("Pago aprobado");
    }

    /// <summary>
    /// Dado que hay mensajes registrados, cuando se vacía el registro,
    /// entonces queda limpio entre pruebas.
    /// </summary>
    [Test]
    public async Task Vaciar_ConMensajesRegistrados_LimpiaElRegistro() {
        // Arrange
        await _servicio.EnviarAsync(Mensaje());

        // Act
        MemoryEmailService.Vaciar();

        // Assert
        MemoryEmailService.Enviados.Should().BeEmpty();
    }
}
