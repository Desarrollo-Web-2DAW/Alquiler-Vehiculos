using Alquiler_Vehiculos.Dto.Clientes;
using Alquiler_Vehiculos.Validators.Clientes;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators.Clientes;

/// <summary>
/// Tests unitarios del validador de modificación de cliente.
/// Requisitos cubiertos: RF-13, RF-14.
/// </summary>
[Category("Unit")]
public class ClienteUpdateValidatorTests {
    private static readonly ClienteUpdateValidator Validador = new();

    private static ClienteUpdateDto DtoValido() {
        return new ClienteUpdateDto {
            Nombre = "Lucía",
            Apellidos = "Navarro Prieto",
            Telefono = "611222333",
            FechaNacimiento = new DateOnly(1995, 3, 21),
            Dni = "12345678Z"
        };
    }

    [TestFixture]
    public class CasosPositivos {
        /// <summary>
        /// Dado un cliente con todos los datos correctos y solo DNI,
        /// cuando se valida, entonces es válido.
        /// </summary>
        [Test]
        public async Task ValidateAsync_DatosCorrectos_EsValido() {
            // Arrange
            var dto = DtoValido();

            // Act
            var resultado = await Validador.ValidateAsync(dto);

            // Assert
            resultado.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Dado un cliente identificado solo con NIE, cuando se valida,
        /// entonces es válido.
        /// </summary>
        [Test]
        public async Task ValidateAsync_SoloNie_EsValido() {
            // Arrange
            var dto = DtoValido() with { Dni = null, Nie = "X1234567L" };

            // Act
            var resultado = await Validador.ValidateAsync(dto);

            // Assert
            resultado.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Dado un cliente identificado solo con pasaporte y con número de permiso,
        /// cuando se valida, entonces es válido.
        /// </summary>
        [Test]
        public async Task ValidateAsync_SoloPasaporteConPermiso_EsValido() {
            // Arrange
            var dto = DtoValido() with {
                Dni = null,
                Pasaporte = "AAA1234565",
                NumeroPermisoConducir = "B1234567"
            };

            // Act
            var resultado = await Validador.ValidateAsync(dto);

            // Assert
            resultado.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Dado un cliente con DNI y NIE a la vez, cuando se valida,
        /// entonces es válido, porque los documentos son independientes.
        /// </summary>
        [Test]
        public async Task ValidateAsync_DniYNieALaVez_EsValido() {
            // Arrange
            var dto = DtoValido() with { Nie = "X1234567L" };

            // Act
            var resultado = await Validador.ValidateAsync(dto);

            // Assert
            resultado.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Dado un teléfono con prefijo y espacios, cuando se valida,
        /// entonces es válido.
        /// </summary>
        [Test]
        public async Task ValidateAsync_TelefonoConPrefijoYEspacios_EsValido() {
            // Arrange
            var dto = DtoValido() with { Telefono = "+34 611 222 333" };

            // Act
            var resultado = await Validador.ValidateAsync(dto);

            // Assert
            resultado.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Dado un nombre y unos apellidos en sus longitudes límite,
        /// cuando se valida, entonces es válido.
        /// </summary>
        [Test]
        public async Task ValidateAsync_LongitudesLimite_EsValido() {
            // Arrange
            var dto = DtoValido() with {
                Nombre = new string('A', 100),
                Apellidos = new string('B', 150)
            };

            // Act
            var resultado = await Validador.ValidateAsync(dto);

            // Assert
            resultado.IsValid.Should().BeTrue();
        }
    }

    [TestFixture]
    public class CasosNegativos {
        /// <summary>
        /// Dado un nombre vacío, en blanco o de una letra, cuando se valida,
        /// entonces hay error en el nombre.
        /// </summary>
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("A")]
        public async Task ValidateAsync_NombreIncorrecto_ErrorEnNombre(string nombre) {
            // Arrange
            var dto = DtoValido() with { Nombre = nombre };

            // Act
            var resultado = await Validador.ValidateAsync(dto);

            // Assert
            resultado.IsValid.Should().BeFalse();
            resultado.ToDictionary().Should().ContainKey("Nombre");
        }

        /// <summary>
        /// Dado un nombre de más de 100 caracteres, cuando se valida,
        /// entonces hay error en el nombre.
        /// </summary>
        [Test]
        public async Task ValidateAsync_NombreDemasiadoLargo_ErrorEnNombre() {
            // Arrange
            var dto = DtoValido() with { Nombre = new string('A', 101) };

            // Act
            var resultado = await Validador.ValidateAsync(dto);

            // Assert
            resultado.IsValid.Should().BeFalse();
            resultado.ToDictionary().Should().ContainKey("Nombre");
        }

        /// <summary>
        /// Dados unos apellidos vacíos o de una sola letra, cuando se valida,
        /// entonces hay error en los apellidos.
        /// </summary>
        [TestCase("")]
        [TestCase("B")]
        public async Task ValidateAsync_ApellidosIncorrectos_ErrorEnApellidos(string apellidos) {
            // Arrange
            var dto = DtoValido() with { Apellidos = apellidos };

            // Act
            var resultado = await Validador.ValidateAsync(dto);

            // Assert
            resultado.IsValid.Should().BeFalse();
            resultado.ToDictionary().Should().ContainKey("Apellidos");
        }

        /// <summary>
        /// Dado un teléfono vacío, corto o con caracteres no permitidos,
        /// cuando se valida, entonces hay error en el teléfono.
        /// </summary>
        [TestCase("")]
        [TestCase("12345")]
        [TestCase("61122233a")]
        [TestCase("611-222-333")]
        public async Task ValidateAsync_TelefonoIncorrecto_ErrorEnTelefono(string telefono) {
            // Arrange
            var dto = DtoValido() with { Telefono = telefono };

            // Act
            var resultado = await Validador.ValidateAsync(dto);

            // Assert
            resultado.IsValid.Should().BeFalse();
            resultado.ToDictionary().Should().ContainKey("Telefono");
        }

        /// <summary>
        /// Dado un teléfono de más de 20 caracteres, cuando se valida,
        /// entonces hay error en el teléfono.
        /// </summary>
        [Test]
        public async Task ValidateAsync_TelefonoDemasiadoLargo_ErrorEnTelefono() {
            // Arrange
            var dto = DtoValido() with { Telefono = new string('6', 21) };

            // Act
            var resultado = await Validador.ValidateAsync(dto);

            // Assert
            resultado.IsValid.Should().BeFalse();
            resultado.ToDictionary().Should().ContainKey("Telefono");
        }

        /// <summary>
        /// Dada una fecha de nacimiento sin rellenar o anterior a 1900,
        /// cuando se valida, entonces hay error en la fecha.
        /// </summary>
        [Test]
        public async Task ValidateAsync_FechaNacimientoSinRellenarOMuyAntigua_ErrorEnFecha() {
            // Arrange
            var sinRellenar = DtoValido() with { FechaNacimiento = default };
            var muyAntigua = DtoValido() with { FechaNacimiento = new DateOnly(1899, 12, 31) };

            // Act
            var resultadoSinRellenar = await Validador.ValidateAsync(sinRellenar);
            var resultadoMuyAntigua = await Validador.ValidateAsync(muyAntigua);

            // Assert
            resultadoSinRellenar.ToDictionary().Should().ContainKey("FechaNacimiento");
            resultadoMuyAntigua.ToDictionary().Should().ContainKey("FechaNacimiento");
        }

        /// <summary>
        /// Dada una fecha de nacimiento de hoy o futura, cuando se valida,
        /// entonces hay error en la fecha.
        /// </summary>
        [Test]
        public async Task ValidateAsync_FechaNacimientoDeHoyOFutura_ErrorEnFecha() {
            // Arrange
            var ahora = DateTime.UtcNow;
            var manana = ahora.AddDays(1);
            var hoy = DtoValido() with { FechaNacimiento = new DateOnly(ahora.Year, ahora.Month, ahora.Day) };
            var futura = DtoValido() with { FechaNacimiento = new DateOnly(manana.Year, manana.Month, manana.Day) };

            // Act
            var resultadoHoy = await Validador.ValidateAsync(hoy);
            var resultadoFutura = await Validador.ValidateAsync(futura);

            // Assert
            resultadoHoy.ToDictionary().Should().ContainKey("FechaNacimiento");
            resultadoFutura.ToDictionary().Should().ContainKey("FechaNacimiento");
        }

        /// <summary>
        /// Dado un DNI con letra incorrecta, longitud errónea o letras en los dígitos,
        /// cuando se valida, entonces hay error en el DNI.
        /// </summary>
        [TestCase("12345678A")]
        [TestCase("1234567Z")]
        [TestCase("ABCDEFGHZ")]
        public async Task ValidateAsync_DniIncorrecto_ErrorEnDni(string dni) {
            // Arrange
            var dto = DtoValido() with { Dni = dni };

            // Act
            var resultado = await Validador.ValidateAsync(dto);

            // Assert
            resultado.IsValid.Should().BeFalse();
            resultado.ToDictionary().Should().ContainKey("Dni");
        }

        /// <summary>
        /// Dado un NIE con letra de control incorrecta o que empieza por otra letra,
        /// cuando se valida, entonces hay error en el NIE.
        /// </summary>
        [TestCase("X1234567A")]
        [TestCase("A1234567L")]
        public async Task ValidateAsync_NieIncorrecto_ErrorEnNie(string nie) {
            // Arrange
            var dto = DtoValido() with { Nie = nie };

            // Act
            var resultado = await Validador.ValidateAsync(dto);

            // Assert
            resultado.IsValid.Should().BeFalse();
            resultado.ToDictionary().Should().ContainKey("Nie");
        }

        /// <summary>
        /// Dado un pasaporte con dígito de control incorrecto o longitud errónea,
        /// cuando se valida, entonces hay error en el pasaporte.
        /// </summary>
        [TestCase("AAA1234566")]
        [TestCase("AAA123456")]
        public async Task ValidateAsync_PasaporteIncorrecto_ErrorEnPasaporte(string pasaporte) {
            // Arrange
            var dto = DtoValido() with { Pasaporte = pasaporte };

            // Act
            var resultado = await Validador.ValidateAsync(dto);

            // Assert
            resultado.IsValid.Should().BeFalse();
            resultado.ToDictionary().Should().ContainKey("Pasaporte");
        }

        /// <summary>
        /// Dado un cliente sin ningún documento, cuando se valida,
        /// entonces falla con el mensaje de que debe indicar al menos uno.
        /// </summary>
        [Test]
        public async Task ValidateAsync_SinNingunDocumento_ErrorDeDocumentoObligatorio() {
            // Arrange
            var dto = DtoValido() with { Dni = null };

            // Act
            var resultado = await Validador.ValidateAsync(dto);

            // Assert
            resultado.IsValid.Should().BeFalse();
            resultado.ToDictionary()["Dni"].Should()
                .Contain("Debe indicar al menos un documento: DNI, NIE o pasaporte");
        }

        /// <summary>
        /// Dado un cliente identificado solo con pasaporte y sin permiso de conducir,
        /// cuando se valida, entonces hay error en el permiso.
        /// </summary>
        [TestCase("")]
        [TestCase("   ")]
        public async Task ValidateAsync_SoloPasaporteSinPermiso_ErrorEnPermiso(string permiso) {
            // Arrange
            var dto = DtoValido() with {
                Dni = null,
                Pasaporte = "AAA1234565",
                NumeroPermisoConducir = permiso
            };

            // Act
            var resultado = await Validador.ValidateAsync(dto);

            // Assert
            resultado.IsValid.Should().BeFalse();
            resultado.ToDictionary().Should().ContainKey("NumeroPermisoConducir");
        }

        /// <summary>
        /// Dado un número de permiso de más de 20 caracteres, cuando se valida,
        /// entonces hay error en el permiso.
        /// </summary>
        [Test]
        public async Task ValidateAsync_PermisoDemasiadoLargo_ErrorEnPermiso() {
            // Arrange
            var dto = DtoValido() with { NumeroPermisoConducir = new string('B', 21) };

            // Act
            var resultado = await Validador.ValidateAsync(dto);

            // Assert
            resultado.IsValid.Should().BeFalse();
            resultado.ToDictionary().Should().ContainKey("NumeroPermisoConducir");
        }
    }
}