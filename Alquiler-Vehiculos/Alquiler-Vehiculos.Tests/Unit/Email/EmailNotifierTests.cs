using Alquiler_Vehiculos.Dto.Email;
using Alquiler_Vehiculos.Models.Email;
using Alquiler_Vehiculos.Services.Email;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Alquiler_Vehiculos.Tests.Unit.Email;

/// <summary>
/// Tests unitarios del notificador de correos (EmailNotifier).
/// Requisitos cubiertos: RF-34 (un correo por evento), RF-35 (nunca lanza
/// excepción) y CU-17 (exactamente un correo por evento, sin duplicados).
/// </summary>
[Category("Unit")]
public class EmailNotifierTests {
    private InMemoryEmailOutboxStore _outbox = null!;
    private EmailNotifier _notificador = null!;

    [SetUp]
    public void Setup() {
        _outbox = new InMemoryEmailOutboxStore();
        _notificador = new EmailNotifier(
            new EmailTemplateRenderer(FakeHostEnvironment.ParaPlantillas()),
            _outbox,
            NullLogger<EmailNotifier>.Instance);
    }

    private static ReservaCreadaEmail DatosReserva() {
        return new ReservaCreadaEmail {
            CodigoReferencia = "REF-001",
            ClienteNombre = "Lucía",
            ClienteEmail = "lucia@ejemplo.com",
            CategoriaPedida = "Furgoneta",
            VehiculoDescripcion = "Ford Transit",
            Matricula = "1234BCD",
            FechaRecogida = new DateOnly(2026, 12, 1),
            FechaDevolucion = new DateOnly(2026, 12, 4),
            Dias = 3,
            Lineas = [new LineaEmailDto("Alquiler de vehículo", 3, 50m, 150m)],
            PrecioTotal = 150m
        };
    }

    private async Task<List<EmailOutboxMessage>> MensajesEnCola() {
        return (await _outbox.ObtenerPendientesAsync(100)).ToList();
    }

    #region Tests de encolado

    /// <summary>
    /// Dado que se notifica un evento de reserva, cuando se encola,
    /// entonces el mensaje queda pendiente con su tipo, destinatario, asunto y cuerpo renderizado.
    /// </summary>
    [Test]
    public async Task NotificarReservaCreada_EventoValido_EncolaElMensajeConSusDatos() {
        // Arrange
        var datos = DatosReserva();

        // Act
        await _notificador.NotificarReservaCreadaAsync(datos);

        // Assert
        var mensajes = await MensajesEnCola();
        mensajes.Should().HaveCount(1);
        var mensaje = mensajes[0];
        mensaje.Tipo.Should().Be(TipoNotificacion.ReservaCreada);
        mensaje.DestinatarioEmail.Should().Be("lucia@ejemplo.com");
        mensaje.DestinatarioNombre.Should().Be("Lucía");
        mensaje.Asunto.Should().Be("Tu reserva REF-001 está creada");
        mensaje.ClaveIdempotencia.Should().Be("ReservaCreada:REF-001");
        mensaje.CuerpoHtml.Should().Contain("Ford Transit");
        mensaje.Estado.Should().Be(EstadoEmailOutbox.Pendiente);
    }

    /// <summary>
    /// Dado que el mismo evento se notifica dos veces, cuando se encola la segunda,
    /// entonces se descarta y el cliente recibe exactamente un correo (CU-17).
    /// </summary>
    [Test]
    public async Task Notificar_MismoEventoDosVeces_EncolaSoloUnMensaje() {
        // Arrange
        var datos = DatosReserva();

        // Act
        await _notificador.NotificarReservaCreadaAsync(datos);
        await _notificador.NotificarReservaCreadaAsync(datos);

        // Assert
        var mensajes = await MensajesEnCola();
        mensajes.Should().HaveCount(1);
    }

    /// <summary>
    /// Dado que la notificación trae su propia clave de idempotencia, cuando se encola,
    /// entonces se usa esa clave en lugar de la generada.
    /// </summary>
    [Test]
    public async Task Notificar_ConClavePropia_UsaEsaClaveComoIdempotencia() {
        // Arrange
        var datos = DatosReserva() with { ClaveIdempotencia = "pago:REF-001:2" };

        // Act
        await _notificador.NotificarReservaCreadaAsync(datos);

        // Assert
        var mensajes = await MensajesEnCola();
        mensajes[0].ClaveIdempotencia.Should().Be("pago:REF-001:2");
    }

    /// <summary>
    /// Dado que existen los once eventos del negocio, cuando se notifican todos,
    /// entonces se encola un mensaje por evento con su plantilla correspondiente.
    /// </summary>
    [Test]
    public async Task Notificar_LosOnceEventos_EncolaUnMensajePorEvento() {
        // Arrange & Act
        await _notificador.NotificarReservaCreadaAsync(DatosReserva());
        await _notificador.NotificarPagoAprobadoAsync(new PagoAprobadoEmail {
            CodigoReferencia = "REF-002", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
            Importe = 150m, MarcaTarjeta = "Visa", UltimosCuatroDigitos = "1234",
            FechaPago = DateTimeOffset.Now, FechaRecogida = DateTimeOffset.Now, VehiculoDescripcion = "Ford Transit"
        });
        await _notificador.NotificarReservaCaducadaAsync(new ReservaCaducadaEmail {
            CodigoReferencia = "REF-003", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
            VehiculoDescripcion = "Ford Transit", FechaRecogida = new DateOnly(2026, 12, 1),
            FechaDevolucion = new DateOnly(2026, 12, 4)
        });
        await _notificador.NotificarReservaCanceladaAsync(new ReservaCanceladaEmail {
            CodigoReferencia = "REF-004", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
            FechaCancelacion = DateTimeOffset.Now, FechaRecogida = new DateOnly(2026, 12, 1),
            FechaDevolucion = new DateOnly(2026, 12, 4), Lineas = [], PrecioTotal = 0m
        });
        await _notificador.NotificarReservaRecogidaAsync(new ReservaRecogidaEmail {
            CodigoReferencia = "REF-005", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
            FechaRecogida = DateTimeOffset.Now, FechaDevolucion = new DateOnly(2026, 12, 4),
            VehiculoDescripcion = "Ford Transit", Matricula = "1234BCD", Color = "Blanco"
        });
        await _notificador.NotificarReservaDevueltaAsync(new ReservaDevueltaEmail {
            CodigoReferencia = "REF-006", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
            FechaDevolucion = DateTimeOffset.Now, Dias = 3, VehiculoDescripcion = "Ford Transit",
            Lineas = [], PrecioTotal = 150m
        });
        await _notificador.NotificarReembolsoAsync(new ReembolsoEmail {
            CodigoReferencia = "REF-007", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
            Motivo = TipoReembolso.PenalizacionNoPresentado, ImporteReembolsado = 100m,
            MarcaTarjeta = "Visa", UltimosCuatroDigitos = "1234"
        });
        await _notificador.NotificarAfectacionTallerAsync(new AfectacionTallerEmail {
            CodigoReferencia = "REF-008", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
            TipoAfectacion = TipoAfectacionTaller.Aplazamiento, Detalle = "Aplazado",
            VehiculoDescripcion = "Ford Transit"
        });
        await _notificador.NotificarAveriaDuranteAlquilerAsync(new AveriaDuranteAlquilerEmail {
            CodigoReferencia = "REF-009", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
            Solucion = SolucionAveria.VehiculoSustitucion, Detalle = "Avería", Instrucciones = "Recoge el sustituto",
            VehiculoDescripcion = "Ford Transit", Matricula = "1234BCD"
        });
        await _notificador.NotificarVehiculoNoListoAsync(new VehiculoNoListoEmail {
            CodigoReferencia = "REF-010", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
            Motivo = "Revisión", Detalle = "Taller", Alternativas = "Otro vehículo",
            FechaRecogida = DateTimeOffset.Now, VehiculoDescripcion = "Ford Transit"
        });
        await _notificador.NotificarDevolucionTardiaAsync(new DevolucionTardiaEmail {
            CodigoReferencia = "REF-011", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
            Detalle = "Retraso", NuevaFechaRecogidaEstimada = "mañana", Alternativas = "Esperar",
            VehiculoDescripcion = "Ford Transit"
        });

        // Assert
        var mensajes = await MensajesEnCola();
        mensajes.Should().HaveCount(11);
        mensajes.Select(m => m.Tipo).Should().BeEquivalentTo(Enum.GetValues<TipoNotificacion>());
    }

    #endregion

    #region Tests de resiliencia (RF-35)

    /// <summary>
    /// Dado que la plantilla falla al renderizar, cuando se notifica,
    /// entonces no se propaga la excepción y la operación de negocio continúa (RF-35).
    /// </summary>
    [Test]
    public async Task Notificar_FalloAlRenderizar_NoLanzaExcepcion() {
        // Arrange
        var notificador = new EmailNotifier(
            new RenderizadorQueFalla(), _outbox, NullLogger<EmailNotifier>.Instance);

        // Act
        Func<Task> act = () => notificador.NotificarReservaCreadaAsync(DatosReserva());

        // Assert
        await act.Should().NotThrowAsync();
        var mensajes = await MensajesEnCola();
        mensajes.Should().BeEmpty();
    }

    /// <summary>
    /// Dado que la cola falla al guardar, cuando se notifica,
    /// entonces no se propaga la excepción (RF-35: el correo nunca rompe la reserva).
    /// </summary>
    [Test]
    public async Task Notificar_FalloEnLaCola_NoLanzaExcepcion() {
        // Arrange
        var notificador = new EmailNotifier(
            new EmailTemplateRenderer(FakeHostEnvironment.ParaPlantillas()),
            new OutboxQueFalla(),
            NullLogger<EmailNotifier>.Instance);

        // Act
        Func<Task> act = () => notificador.NotificarReservaCreadaAsync(DatosReserva());

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    private sealed class RenderizadorQueFalla : IEmailTemplateRenderer {
        public Task<string> RenderizarAsync(TipoNotificacion tipo, object datos, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("fallo al renderizar");
    }

    private sealed class OutboxQueFalla : IEmailOutboxStore {
        public Task<bool> AgregarAsync(EmailOutboxMessage mensaje, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("fallo al encolar");

        public Task<IReadOnlyList<EmailOutboxMessage>> ObtenerPendientesAsync(int lote, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EmailOutboxMessage>>([]);

        public Task ActualizarAsync(EmailOutboxMessage mensaje, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
