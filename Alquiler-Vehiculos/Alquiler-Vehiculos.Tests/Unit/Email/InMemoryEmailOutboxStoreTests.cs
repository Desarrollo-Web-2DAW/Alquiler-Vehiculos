using Alquiler_Vehiculos.Models.Email;
using Alquiler_Vehiculos.Services.Email;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Email;

/// <summary>
/// Tests unitarios de la cola de envío en memoria (InMemoryEmailOutboxStore).
/// Requisitos cubiertos: RF-35 (el error queda registrado para reintentarlo) y
/// CU-17 (la clave de idempotencia impide duplicados).
/// </summary>
[Category("Unit")]
public class InMemoryEmailOutboxStoreTests {
    private InMemoryEmailOutboxStore _outbox = null!;

    [SetUp]
    public void Setup() {
        _outbox = new InMemoryEmailOutboxStore();
    }

    private static EmailOutboxMessage Mensaje(string clave, EstadoEmailOutbox estado = EstadoEmailOutbox.Pendiente,
        DateTimeOffset? proximoIntento = null) {
        return new EmailOutboxMessage {
            Tipo = TipoNotificacion.ReservaCreada,
            ClaveIdempotencia = clave,
            DestinatarioEmail = "lucia@ejemplo.com",
            DestinatarioNombre = "Lucía",
            Asunto = "Asunto",
            CuerpoHtml = "<p>Hola</p>",
            Estado = estado,
            ProximoIntento = proximoIntento
        };
    }

    #region Tests de encolado

    /// <summary>
    /// Dado que la cola está vacía, cuando se añade un mensaje,
    /// entonces se acepta y queda pendiente de envío.
    /// </summary>
    [Test]
    public async Task Agregar_MensajeNuevo_DevuelveTrueYQuedaPendiente() {
        // Arrange
        var mensaje = Mensaje("ReservaCreada:REF-001");

        // Act
        var añadido = await _outbox.AgregarAsync(mensaje);

        // Assert
        añadido.Should().BeTrue();
        var pendientes = await _outbox.ObtenerPendientesAsync(10);
        pendientes.Should().HaveCount(1);
    }

    /// <summary>
    /// Dado que ya hay un mensaje con la misma clave, cuando se añade otro igual,
    /// entonces se descarta: solo hay un correo por evento (CU-17).
    /// </summary>
    [Test]
    public async Task Agregar_ClaveDuplicada_DevuelveFalseYSinDuplicados() {
        // Arrange
        await _outbox.AgregarAsync(Mensaje("ReservaCreada:REF-001"));

        // Act
        var añadido = await _outbox.AgregarAsync(Mensaje("ReservaCreada:REF-001"));

        // Assert
        añadido.Should().BeFalse();
        var pendientes = await _outbox.ObtenerPendientesAsync(10);
        pendientes.Should().HaveCount(1);
    }

    #endregion

    #region Tests de consulta y actualización

    /// <summary>
    /// Dado que hay mensajes con retraso, cuando se piden los pendientes,
    /// entonces solo devuelve los que ya vencen, por orden de creación y hasta el lote.
    /// </summary>
    [Test]
    public async Task ObtenerPendientes_ConRetrasos_DevuelveSoloLosVencidosPorOrdenYLote() {
        // Arrange
        var ahora = DateTimeOffset.UtcNow;
        await _outbox.AgregarAsync(Mensaje("k1", proximoIntento: ahora.AddMinutes(5)));
        await _outbox.AgregarAsync(Mensaje("k2", proximoIntento: ahora.AddSeconds(-1)));
        await _outbox.AgregarAsync(Mensaje("k3", proximoIntento: ahora.AddSeconds(-1)));

        // Act
        var loteCompleto = await _outbox.ObtenerPendientesAsync(10);
        var loteLimitado = await _outbox.ObtenerPendientesAsync(1);

        // Assert
        loteCompleto.Select(m => m.ClaveIdempotencia).Should().Equal("k2", "k3");
        loteLimitado.Should().HaveCount(1);
    }

    /// <summary>
    /// Dado que un mensaje se marca como enviado o fallido, cuando se piden pendientes,
    /// entonces ya no aparece (el estado actualizado se conserva).
    /// </summary>
    [Test]
    public async Task Actualizar_MarcaEnvioYFallo_YaNoQuedanPendientes() {
        // Arrange
        await _outbox.AgregarAsync(Mensaje("k1"));
        await _outbox.AgregarAsync(Mensaje("k2"));
        var mensajes = await _outbox.ObtenerPendientesAsync(10);
        mensajes[0].Estado = EstadoEmailOutbox.Enviado;
        mensajes[0].FechaEnvio = DateTimeOffset.UtcNow;
        mensajes[1].Estado = EstadoEmailOutbox.Fallido;
        mensajes[1].Intentos = 5;

        // Act
        await _outbox.ActualizarAsync(mensajes[0]);
        await _outbox.ActualizarAsync(mensajes[1]);

        // Assert
        var pendientes = await _outbox.ObtenerPendientesAsync(10);
        pendientes.Should().BeEmpty();
    }

    #endregion
}
