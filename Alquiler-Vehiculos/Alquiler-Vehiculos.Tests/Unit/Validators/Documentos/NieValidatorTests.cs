using Alquiler_Vehiculos.Validators.Documentos;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators.Documentos;

/// <summary>
/// Tests unitarios del validador de NIE español.
/// Requisito cubierto: RF-16.
/// </summary>
[Category("Unit")]
public class NieValidatorTests {
    [TestFixture]
    public class CasosPositivos {
        /// <summary>
        /// Dado un NIE con formato correcto y letra de control coincidente,
        /// cuando se valida, entonces es válido.
        /// </summary>
        [TestCase("X1234567L")]
        [TestCase("Y1234567X")]
        [TestCase("Z1234567R")]
        [TestCase("X0000000T")]
        [TestCase("Z9999999H")]
        public void EsValido_NieCorrecto_DevuelveTrue(string nie) {
            // Arrange

            // Act
            var resultado = NieValidator.EsValido(nie);

            // Assert
            resultado.Should().BeTrue();
        }

        /// <summary>
        /// Dado un NIE correcto escrito en minúscula o con espacios alrededor,
        /// cuando se valida, entonces es válido.
        /// </summary>
        [TestCase("x1234567l")]
        [TestCase("  Y1234567X  ")]
        public void EsValido_NieConMinusculasOEspacios_DevuelveTrue(string nie) {
            // Arrange

            // Act
            var resultado = NieValidator.EsValido(nie);

            // Assert
            resultado.Should().BeTrue();
        }
    }

    [TestFixture]
    public class CasosNegativos {
        /// <summary>
        /// Dado un NIE cuya letra de control no corresponde al número,
        /// cuando se valida, entonces no es válido.
        /// </summary>
        [TestCase("X1234567A")]
        [TestCase("Y1234567A")]
        [TestCase("Z1234567A")]
        public void EsValido_LetraDeControlIncorrecta_DevuelveFalse(string nie) {
            // Arrange

            // Act
            var resultado = NieValidator.EsValido(nie);

            // Assert
            resultado.Should().BeFalse();
        }

        /// <summary>
        /// Dado un texto que no empieza por X, Y o Z, incluido un DNI válido,
        /// cuando se valida, entonces no es válido.
        /// </summary>
        [TestCase("A1234567L")]
        [TestCase("W1234567L")]
        [TestCase("12345678Z")]
        public void EsValido_LetraInicialNoPermitida_DevuelveFalse(string nie) {
            // Arrange

            // Act
            var resultado = NieValidator.EsValido(nie);

            // Assert
            resultado.Should().BeFalse();
        }

        /// <summary>
        /// Dado un texto vacío o en blanco, cuando se valida,
        /// entonces no es válido.
        /// </summary>
        [TestCase("")]
        [TestCase("   ")]
        public void EsValido_VacioOEnBlanco_DevuelveFalse(string nie) {
            // Arrange

            // Act
            var resultado = NieValidator.EsValido(nie);

            // Assert
            resultado.Should().BeFalse();
        }

        /// <summary>
        /// Dado un NIE nulo, cuando se valida, entonces no es válido.
        /// </summary>
        [Test]
        public void EsValido_Nulo_DevuelveFalse() {
            // Arrange

            // Act
            var resultado = NieValidator.EsValido(null);

            // Assert
            resultado.Should().BeFalse();
        }

        /// <summary>
        /// Dado un texto con longitud distinta de nueve caracteres,
        /// cuando se valida, entonces no es válido.
        /// </summary>
        [TestCase("X123456L")]
        [TestCase("X12345678L")]
        [TestCase("X1234567")]
        public void EsValido_LongitudIncorrecta_DevuelveFalse(string nie) {
            // Arrange

            // Act
            var resultado = NieValidator.EsValido(nie);

            // Assert
            resultado.Should().BeFalse();
        }

        /// <summary>
        /// Dado un NIE con letras donde van los dígitos,
        /// cuando se valida, entonces no es válido.
        /// </summary>
        [TestCase("X12345A7L")]
        [TestCase("XABCDEFGL")]
        public void EsValido_LetrasEnLosDigitos_DevuelveFalse(string nie) {
            // Arrange

            // Act
            var resultado = NieValidator.EsValido(nie);

            // Assert
            resultado.Should().BeFalse();
        }
    }
}