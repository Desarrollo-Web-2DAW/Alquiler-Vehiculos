using Alquiler_Vehiculos.Dto.Email;
using Alquiler_Vehiculos.Models.Email;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Email;

/// <summary>
/// Tests unitarios de los DTOs de notificación: asuntos, textos de las
/// enumeraciones y valores por defecto que se usan en las plantillas (RF-34).
/// </summary>
[Category("Unit")]
public class NotificacionEmailDtoTests {
    /// <summary>
    /// Dado que cada evento tiene su asunto, cuando se consulta,
    /// entonces incluye el código de referencia de la reserva.
    /// </summary>
    [Test]
    public void Asunto_ReservaCreada_IncluyeElCodigoDeReferencia() {
        // Arrange
        var creado = new ReservaCreadaEmail {
            CodigoReferencia = "REF-001", ClienteNombre = "Lucía", ClienteEmail = "l@e.com",
            CategoriaPedida = "Suv", VehiculoDescripcion = "Nissan Ariya", Matricula = "1234BCD",
            FechaRecogida = new DateOnly(2026, 12, 1), FechaDevolucion = new DateOnly(2026, 12, 4),
            Dias = 3, Lineas = [], PrecioTotal = 100m
        };

        // Act & Assert
        creado.Asunto.Should().Be("Tu reserva REF-001 está creada");
    }

    /// <summary>
    /// Dado que un reembolso puede ser por anulación o por no presentado,
    /// cuando se consulta el motivo en texto,
    /// entonces se describe en castellano (y con un valor desconocido, genérico).
    /// </summary>
    [Test]
    public void Reembolso_MotivoTexto_CubreTodosLosCasos() {
        // Arrange
        ReembolsoEmail Con(TipoReembolso motivo) => new() {
            CodigoReferencia = "REF-001", ClienteNombre = "Lucía", ClienteEmail = "l@e.com",
            Motivo = motivo, ImporteReembolsado = 100m, MarcaTarjeta = "Visa", UltimosCuatroDigitos = "1234"
        };

        // Act & Assert
        Con(TipoReembolso.AnulacionReserva).MotivoTexto.Should().Be("Anulación de la reserva");
        Con(TipoReembolso.PenalizacionNoPresentado).MotivoTexto.Should()
            .Be("Penalización por no presentarse a la recogida");
        Con((TipoReembolso)999).MotivoTexto.Should().Be("Reembolso");
    }

    /// <summary>
    /// Dado que una afectación de taller puede ser cambio, cancelación o aplazamiento,
    /// cuando se consulta el tipo en texto,
    /// entonces se describe en castellano (y con un valor desconocido, genérico).
    /// </summary>
    [Test]
    public void AfectacionTaller_TipoAfectacionTexto_CubreTodosLosCasos() {
        // Arrange
        AfectacionTallerEmail Con(TipoAfectacionTaller tipo) => new() {
            CodigoReferencia = "REF-001", ClienteNombre = "Lucía", ClienteEmail = "l@e.com",
            TipoAfectacion = tipo, Detalle = "Detalle", VehiculoDescripcion = "Ford Transit"
        };

        // Act & Assert
        Con(TipoAfectacionTaller.CambioVehiculo).TipoAfectacionTexto.Should().Be("Cambio de vehículo");
        Con(TipoAfectacionTaller.CancelacionReserva).TipoAfectacionTexto.Should().Be("Cancelación de la reserva");
        Con(TipoAfectacionTaller.Aplazamiento).TipoAfectacionTexto.Should().Be("Aplazamiento de la recogida");
        Con((TipoAfectacionTaller)999).TipoAfectacionTexto.Should().Be("Cambio en tu reserva");
    }

    /// <summary>
    /// Dado que una avería puede resolverse de tres formas, cuando se consulta
    /// la solución en texto, entonces se describe en castellano (y con un valor
    /// desconocido, genérico).
    /// </summary>
    [Test]
    public void Averia_SolucionTexto_CubreTodosLosCasos() {
        // Arrange
        AveriaDuranteAlquilerEmail Con(SolucionAveria solucion) => new() {
            CodigoReferencia = "REF-001", ClienteNombre = "Lucía", ClienteEmail = "l@e.com",
            Solucion = solucion, Detalle = "Detalle", Instrucciones = "Instrucciones",
            VehiculoDescripcion = "Ford Transit", Matricula = "1234BCD"
        };

        // Act & Assert
        Con(SolucionAveria.FinAnticipado).SolucionTexto.Should().Be("Fin anticipado del alquiler");
        Con(SolucionAveria.VehiculoSustitucion).SolucionTexto.Should().Be("Vehículo de sustitución");
        Con(SolucionAveria.AplazamientoDevolucion).SolucionTexto.Should().Be("Aplazamiento de la devolución");
        Con((SolucionAveria)999).SolucionTexto.Should().Be("Solución en curso");
    }

    /// <summary>
    /// Dado que los DTOs traen textos por defecto, cuando se crean sin personalizarlos,
    /// entonces los avisos y motivos opcionales tienen su valor inicial.
    /// </summary>
    [Test]
    public void ValoresPorDefecto_SeUsanCuandoNoSePersonalizan() {
        // Arrange
        var pago = new PagoAprobadoEmail {
            CodigoReferencia = "REF-001", ClienteNombre = "Lucía", ClienteEmail = "l@e.com",
            Importe = 100m, MarcaTarjeta = "Visa", UltimosCuatroDigitos = "1234",
            FechaPago = DateTimeOffset.Now, FechaRecogida = DateTimeOffset.Now, VehiculoDescripcion = "Ford Transit"
        };
        var caducada = new ReservaCaducadaEmail {
            CodigoReferencia = "REF-002", ClienteNombre = "Lucía", ClienteEmail = "l@e.com",
            VehiculoDescripcion = "Ford Transit", FechaRecogida = new DateOnly(2026, 12, 1),
            FechaDevolucion = new DateOnly(2026, 12, 4)
        };
        var devuelta = new ReservaDevueltaEmail {
            CodigoReferencia = "REF-003", ClienteNombre = "Lucía", ClienteEmail = "l@e.com",
            FechaDevolucion = DateTimeOffset.Now, Dias = 3, VehiculoDescripcion = "Ford Transit",
            Lineas = [], PrecioTotal = 100m
        };
        var reembolso = new ReembolsoEmail {
            CodigoReferencia = "REF-004", ClienteNombre = "Lucía", ClienteEmail = "l@e.com",
            Motivo = TipoReembolso.AnulacionReserva, ImporteReembolsado = 100m,
            MarcaTarjeta = "Visa", UltimosCuatroDigitos = "1234"
        };
        var recogida = new ReservaRecogidaEmail {
            CodigoReferencia = "REF-005", ClienteNombre = "Lucía", ClienteEmail = "l@e.com",
            FechaRecogida = DateTimeOffset.Now, FechaDevolucion = new DateOnly(2026, 12, 4),
            VehiculoDescripcion = "Ford Transit", Matricula = "1234BCD", Color = "Blanco"
        };

        // Act & Assert
        pago.InstruccionesRecogida.Should().Contain("documento de identidad");
        caducada.Motivo.Should().Contain("30 minutos");
        devuelta.MensajeValoracion.Should().Contain("valorar");
        reembolso.PlazoEstimado.Should().Contain("días laborables");
        reembolso.ImporteRetenido.Should().Be(0m);
        recogida.Instrucciones.Should().Contain("permiso de conducir");
    }
}
