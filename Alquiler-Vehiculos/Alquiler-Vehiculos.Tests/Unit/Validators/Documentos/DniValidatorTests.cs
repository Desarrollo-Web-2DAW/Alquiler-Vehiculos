using Alquiler_Vehiculos.Validators.Documentos;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators.Documentos;

/// <summary>
/// Tests unitarios del validador de DNI español.
/// Requisito cubierto: RF-16.
/// </summary>
[Category("Unit")]
public class DniValidatorTests {
    [TestFixture]
    public class CasosPositivos {
        /// <summary>
        /// Dado un DNI con formato correcto y letra de control coincidente,
        /// cuando se valida, entonces es válido.
        /// </summary>
        [TestCase("12345678Z")]
        [TestCase("00000000T")]
        [TestCase("87654321X")]
        [TestCase("99999999R")]
        public void EsValido_DniCorrecto_DevuelveTrue(string dni) {
            // Arrange

            // Act
            var resultado = DniValidator.EsValido(dni);

            // Assert
            resultado.Should().BeTrue();
        }

        /// <summary>
        /// Dado un DNI correcto escrito en minúscula o con espacios alrededor,
        /// cuando se valida, entonces es válido.
        /// </summary>
        [TestCase("12345678z")]
        [TestCase("  12345678Z  ")]
        public void EsValido_DniConMinusculasOEspacios_DevuelveTrue(string dni) {
            // Arrange

            // Act
            var resultado = DniValidator.EsValido(dni);

            // Assert
            resultado.Should().BeTrue();
        }

        /// <summary>
        /// Dado un número, cuando se calcula su letra de control,
        /// entonces devuelve la letra que le corresponde.
        /// </summary>
        [TestCase(12345678, 'Z')]
        [TestCase(0, 'T')]
        [TestCase(23, 'T')]
        [TestCase(99999999, 'R')]
        public void CalcularLetra_NumeroConocido_DevuelveSuLetra(int numero, char esperada) {
            // Arrange

            // Act
            var letra = DniValidator.CalcularLetra(numero);

            // Assert
            letra.Should().Be(esperada);
        }
    }

    [TestFixture]
    public class CasosNegativos {
        /// <summary>
        /// Dado un DNI cuya letra no corresponde al número,
        /// cuando se valida, entonces no es válido.
        /// </summary>
        [TestCase("12345678A")]
        [TestCase("00000000A")]
        [TestCase("87654321Z")]
        public void EsValido_LetraIncorrecta_DevuelveFalse(string dni) {
            // Arrange

            // Act
            var resultado = DniValidator.EsValido(dni);

            // Assert
            resultado.Should().BeFalse();
        }

        /// <summary>
        /// Dado un texto vacío, en blanco o nulo, cuando se valida,
        /// entonces no es válido.
        /// </summary>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void EsValido_VacioOEnBlanco_DevuelveFalse(string? dni) {
            // Arrange

            // Act
            var resultado = DniValidator.EsValido(dni);

            // Assert
            resultado.Should().BeFalse();
        }

        /// <summary>
        /// Dado un texto con longitud distinta de nueve caracteres,
        /// cuando se valida, entonces no es válido.
        /// </summary>
        [TestCase("1234567Z")]
        [TestCase("123456789Z")]
        [TestCase("12345678")]
        public void EsValido_LongitudIncorrecta_DevuelveFalse(string dni) {
            // Arrange

            // Act
            var resultado = DniValidator.EsValido(dni);

            // Assert
            resultado.Should().BeFalse();
        }

        /// <summary>
        /// Dado un texto de nueve caracteres con letras donde van los dígitos,
        /// cuando se valida, entonces no es válido.
        /// </summary>
        [TestCase("1234567AZ")]
        [TestCase("ABCDEFGHZ")]
        public void EsValido_LetrasEnLosDigitos_DevuelveFalse(string dni) {
            // Arrange

            // Act
            var resultado = DniValidator.EsValido(dni);

            // Assert
            resultado.Should().BeFalse();
        }
    }
}