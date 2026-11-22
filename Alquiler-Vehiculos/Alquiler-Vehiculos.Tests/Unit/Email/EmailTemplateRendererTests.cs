using System.Text.RegularExpressions;
using Alquiler_Vehiculos.Dto.Email;
using Alquiler_Vehiculos.Models.Email;
using Alquiler_Vehiculos.Services.Email;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Email;

/// <summary>
/// Tests unitarios del renderizador de plantillas de correo (EmailTemplateRenderer).
/// Requisito cubierto: RF-34 (el contenido de cada correo lleva los datos del evento).
/// </summary>
[Category("Unit")]
public class EmailTemplateRendererTests {
    private EmailTemplateRenderer _renderizador = null!;

    [SetUp]
    public void Setup() {
        _renderizador = new EmailTemplateRenderer(FakeHostEnvironment.ParaPlantillas());
    }

    private static ReservaCreadaEmail DatosReservaCreada() {
        return new ReservaCreadaEmail {
            CodigoReferencia = "REF-001",
            ClienteNombre = "Lucía",
            ClienteEmail = "lucia@ejemplo.com",
            OficinaNombre = "Oficina Leganés",
            OficinaDireccion = "Mayor 1",
            OficinaTelefono = "910000000",
            OficinaHorario = "L-V 9:00-20:00",
            CategoriaPedida = "Furgoneta",
            VehiculoDescripcion = "Ford Transit",
            Matricula = "1234BCD",
            FechaRecogida = new DateOnly(2026, 12, 1),
            FechaDevolucion = new DateOnly(2026, 12, 4),
            Dias = 3,
            Lineas = [
                new LineaEmailDto("Alquiler de vehículo", 3, 50m, 150m),
                new LineaEmailDto("Póliza básica", 3, 0m, 0m)
            ],
            PrecioTotal = 1234.5m
        };
    }

    #region Tests de marcadores

    /// <summary>
    /// Dado que hay una plantilla con marcadores y una lista, cuando se renderiza,
    /// entonces se sustituyen los datos, se repiten las filas de la lista y no
    /// queda ningún marcador sin resolver.
    /// </summary>
    [Test]
    public async Task Renderizar_ReservaCreada_SustituyePlaceholdersYLineas() {
        // Arrange
        var datos = DatosReservaCreada();

        // Act
        var html = await _renderizador.RenderizarAsync(TipoNotificacion.ReservaCreada, datos);

        // Assert
        html.Should().Contain("REF-001");
        html.Should().Contain("Lucía");
        html.Should().Contain("Ford Transit");
        html.Should().Contain("01/12/2026");
        html.Should().Contain("1.234,50");
        Regex.Matches(html, "Alquiler de vehículo").Count.Should().Be(1);
        Regex.Matches(html, "Póliza básica").Count.Should().Be(1);
        html.Should().NotContain("{{");
    }

    /// <summary>
    /// Dado que los datos llevan números y fechas, cuando se renderiza,
    /// entonces se formatean en castellano (decimales con dos cifras y fechas dd/MM/yyyy).
    /// </summary>
    [Test]
    public async Task Renderizar_DecimalesYFechas_SeFormateanEnCastellano() {
        // Arrange
        var datos = DatosReservaCreada() with {
            FechaRecogida = new DateOnly(2027, 1, 5),
            PrecioTotal = 9876.5m
        };

        // Act
        var html = await _renderizador.RenderizarAsync(TipoNotificacion.ReservaCreada, datos);

        // Assert
        html.Should().Contain("05/01/2027");
        html.Should().Contain("9.876,50");
    }

    /// <summary>
    /// Dado que un marcador apunta a valores de otros tipos, cuando se renderiza,
    /// entonces cada tipo se formatea (Sí/No, decimal, fecha con hora y texto).
    /// </summary>
    [Test]
    public async Task Renderizar_MarcadoresDeDistintosTipos_SeFormateanSegunSuTipo() {
        // Arrange: cada propiedad coincide con un marcador de reserva-creada.html
        var datos = new {
            Dias = true,
            PrecioTotal = 1234.5,
            Matricula = 1.5f,
            FechaRecogida = new DateTime(2026, 12, 1, 17, 30, 0),
            CategoriaPedida = Guid.Empty
        };

        // Act
        var html = await _renderizador.RenderizarAsync(TipoNotificacion.ReservaCreada, datos);

        // Assert
        html.Should().Contain(">Sí<");
        html.Should().Contain("1.234,50");
        html.Should().Contain("1,50");
        html.Should().Contain("01/12/2026 17:30");
        html.Should().Contain(Guid.Empty.ToString());
    }

    /// <summary>
    /// Dado que se renderiza sin datos conocidos, cuando faltan propiedades y colecciones,
    /// entonces los marcadores se vacían y el bloque de lista no deja rastro.
    /// </summary>
    [Test]
    public async Task Renderizar_SinPropiedadesConocidas_DejaLosMarcadoresVacios() {
        // Arrange
        var datos = new { };

        // Act
        var html = await _renderizador.RenderizarAsync(TipoNotificacion.ReservaCreada, datos);

        // Assert
        html.Should().NotContain("{{");
        html.Should().NotContain("Alquiler de vehículo");
    }

    /// <summary>
    /// Dado que la colección del bloque no es una lista, cuando se renderiza,
    /// entonces el bloque se elimina sin error (tampoco si los datos son un texto).
    /// </summary>
    [Test]
    public async Task Renderizar_ColeccionQueNoEsLista_EliminaElBloque() {
        // Arrange
        var conTexto = new { Lineas = "no soy una lista" };

        // Act
        var resultadoTexto = await _renderizador.RenderizarAsync(TipoNotificacion.ReservaCreada, conTexto);
        var resultadoCadena = await _renderizador.RenderizarAsync(TipoNotificacion.ReservaCreada, "tampoco soy datos");

        // Assert
        resultadoTexto.Should().NotContain("{{");
        resultadoCadena.Should().NotContain("{{");
    }

    #endregion

    #region Tests de plantillas

    /// <summary>
    /// Dado que existen las once plantillas de notificación, cuando se pide cada una,
    /// entonces todas se cargan y renderizan sin error (la caché de plantillas las reutiliza).
    /// </summary>
    [Test]
    public async Task Renderizar_TodasLasPlantillas_SeCarganYRenderizan() {
        // Arrange
        foreach (var tipo in Enum.GetValues<TipoNotificacion>()) {
            var datos = DatosPara(tipo);

            // Act
            var html = await _renderizador.RenderizarAsync(tipo, datos);

            // Assert
            html.Should().NotContain("{{", $"la plantilla {tipo} deja marcadores sin resolver");
            html.Should().Contain("Lucía", $"la plantilla {tipo} no sustituye los datos");
        }
    }

    /// <summary>
    /// Dado que cada tipo de notificación tiene su plantilla, cuando se pide su nombre,
    /// entonces se obtiene el fichero en kebab-case.
    /// </summary>
    [Test]
    public void NombrePlantilla_CadaTipo_DevuelveSuFicheroEnKebabCase() {
        // Arrange & Act & Assert
        EmailTemplateRenderer.NombrePlantilla(TipoNotificacion.ReservaCreada).Should().Be("reserva-creada.html");
        EmailTemplateRenderer.NombrePlantilla(TipoNotificacion.PagoAprobado).Should().Be("pago-aprobado.html");
        EmailTemplateRenderer.NombrePlantilla(TipoNotificacion.Reembolso).Should().Be("reembolso.html");
        EmailTemplateRenderer.NombrePlantilla(TipoNotificacion.AveriaDuranteAlquiler).Should().Be("averia-durante-alquiler.html");
    }

    #endregion

    private static object DatosPara(TipoNotificacion tipo) {
        return tipo switch {
            TipoNotificacion.ReservaCreada => DatosReservaCreada(),
            TipoNotificacion.PagoAprobado => new PagoAprobadoEmail {
                CodigoReferencia = "REF-001", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
                Importe = 100m, MarcaTarjeta = "Visa", UltimosCuatroDigitos = "1234",
                FechaPago = DateTimeOffset.Now, FechaRecogida = DateTimeOffset.Now, VehiculoDescripcion = "Ford Transit"
            },
            TipoNotificacion.ReservaCaducada => new ReservaCaducadaEmail {
                CodigoReferencia = "REF-001", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
                VehiculoDescripcion = "Ford Transit", FechaRecogida = new DateOnly(2026, 12, 1),
                FechaDevolucion = new DateOnly(2026, 12, 4)
            },
            TipoNotificacion.ReservaCancelada => new ReservaCanceladaEmail {
                CodigoReferencia = "REF-001", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
                FechaCancelacion = DateTimeOffset.Now, FechaRecogida = new DateOnly(2026, 12, 1),
                FechaDevolucion = new DateOnly(2026, 12, 4), Lineas = [], PrecioTotal = 100m
            },
            TipoNotificacion.ReservaRecogida => new ReservaRecogidaEmail {
                CodigoReferencia = "REF-001", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
                FechaRecogida = DateTimeOffset.Now, FechaDevolucion = new DateOnly(2026, 12, 4),
                VehiculoDescripcion = "Ford Transit", Matricula = "1234BCD", Color = "Blanco"
            },
            TipoNotificacion.ReservaDevuelta => new ReservaDevueltaEmail {
                CodigoReferencia = "REF-001", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
                FechaDevolucion = DateTimeOffset.Now, Dias = 3, VehiculoDescripcion = "Ford Transit",
                Lineas = [], PrecioTotal = 150m
            },
            TipoNotificacion.Reembolso => new ReembolsoEmail {
                CodigoReferencia = "REF-001", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
                Motivo = TipoReembolso.AnulacionReserva, ImporteReembolsado = 150m,
                MarcaTarjeta = "Visa", UltimosCuatroDigitos = "1234"
            },
            TipoNotificacion.AfectacionTaller => new AfectacionTallerEmail {
                CodigoReferencia = "REF-001", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
                TipoAfectacion = TipoAfectacionTaller.CambioVehiculo, Detalle = "Cambio de vehículo",
                VehiculoDescripcion = "Ford Transit"
            },
            TipoNotificacion.AveriaDuranteAlquiler => new AveriaDuranteAlquilerEmail {
                CodigoReferencia = "REF-001", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
                Solucion = SolucionAveria.FinAnticipado, Detalle = "Avería", Instrucciones = "Devuelve el vehículo",
                VehiculoDescripcion = "Ford Transit", Matricula = "1234BCD"
            },
            TipoNotificacion.VehiculoNoListo => new VehiculoNoListoEmail {
                CodigoReferencia = "REF-001", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
                Motivo = "Revisión", Detalle = "El coche está en el taller", Alternativas = "Otro vehículo",
                FechaRecogida = DateTimeOffset.Now, VehiculoDescripcion = "Ford Transit"
            },
            _ => new DevolucionTardiaEmail {
                CodigoReferencia = "REF-001", ClienteNombre = "Lucía", ClienteEmail = "lucia@ejemplo.com",
                Detalle = "Devolución tardía", NuevaFechaRecogidaEstimada = "mañana",
                Alternativas = "Esperar", VehiculoDescripcion = "Ford Transit"
            }
        };
    }
}
