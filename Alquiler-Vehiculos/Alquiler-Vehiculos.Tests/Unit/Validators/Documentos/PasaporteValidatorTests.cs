using Alquiler_Vehiculos.Validators.Documentos;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators.Documentos;

/// <summary>
/// Tests unitarios del validador de pasaporte con dígito de control de la MRZ.
/// Requisito cubierto: RF-18. El formato es decisión nuestra, pendiente de confirmar con el profesor.
/// </summary>
[Category("Unit")]
public class PasaporteValidatorTests {
    [TestFixture]
    public class CasosPositivos {
        /// <summary>
        /// Dado un pasaporte de nueve caracteres con su dígito de control correcto,
        /// cuando se valida, entonces es válido.
        /// </summary>
        [TestCase("AAA1234565")]
        [TestCase("ABC1234560")]
        [TestCase("1234567897")]
        [TestCase("0000000000")]
        [TestCase("AB<<<<<<<3")]
        public void EsValido_PasaporteCorrecto_DevuelveTrue(string pasaporte) {
            // Arrange

            // Act
            var resultado = PasaporteValidator.EsValido(pasaporte);

            // Assert
            resultado.Should().BeTrue();
        }

        /// <summary>
        /// Dado un pasaporte correcto escrito en minúscula o con espacios alrededor,
        /// cuando se valida, entonces es válido.
        /// </summary>
        [TestCase("aaa1234565")]
        [TestCase("  AAA1234565  ")]
        public void EsValido_PasaporteConMinusculasOEspacios_DevuelveTrue(string pasaporte) {
            // Arrange

            // Act
            var resultado = PasaporteValidator.EsValido(pasaporte);

            // Assert
            resultado.Should().BeTrue();
        }

        /// <summary>
        /// Dada una cadena válida, cuando se calcula su dígito de control,
        /// entonces devuelve el dígito que le corresponde.
        /// </summary>
        [TestCase("800101", 4)]
        [TestCase("AAA123456", 5)]
        [TestCase("ABC123456", 0)]
        [TestCase("123456789", 7)]
        [TestCase("<<<", 0)]
        public void CalcularDigitoControl_CadenaValida_DevuelveSuDigito(string campo, int esperado) {
            // Arrange

            // Act
            var digito = PasaporteValidator.CalcularDigitoControl(campo);

            // Assert
            digito.Should().Be(esperado);
        }
    }

    [TestFixture]
    public class CasosNegativos {
        /// <summary>
        /// Dado un pasaporte cuyo dígito de control no corresponde,
        /// cuando se valida, entonces no es válido.
        /// </summary>
        [TestCase("AAA1234566")]
        [TestCase("ABC1234561")]
        [TestCase("1234567890")]
        public void EsValido_DigitoDeControlIncorrecto_DevuelveFalse(string pasaporte) {
            // Arrange

            // Act
            var resultado = PasaporteValidator.EsValido(pasaporte);

            // Assert
            resultado.Should().BeFalse();
        }

        /// <summary>
        /// Dado un texto vacío o en blanco, cuando se valida,
        /// entonces no es válido.
        /// </summary>
        [TestCase("")]
        [TestCase("   ")]
        public void EsValido_VacioOEnBlanco_DevuelveFalse(string pasaporte) {
            // Arrange

            // Act
            var resultado = PasaporteValidator.EsValido(pasaporte);

            // Assert
            resultado.Should().BeFalse();
        }

        /// <summary>
        /// Dado un pasaporte nulo, cuando se valida, entonces no es válido.
        /// </summary>
        [Test]
        public void EsValido_Nulo_DevuelveFalse() {
            // Arrange

            // Act
            var resultado = PasaporteValidator.EsValido(null);

            // Assert
            resultado.Should().BeFalse();
        }

        /// <summary>
        /// Dado un texto con longitud distinta de diez caracteres,
        /// cuando se valida, entonces no es válido.
        /// </summary>
        [TestCase("AAA123456")]
        [TestCase("AAA12345655")]
        [TestCase("AAA123")]
        public void EsValido_LongitudIncorrecta_DevuelveFalse(string pasaporte) {
            // Arrange

            // Act
            var resultado = PasaporteValidator.EsValido(pasaporte);

            // Assert
            resultado.Should().BeFalse();
        }

        /// <summary>
        /// Dado un pasaporte cuyo último carácter no es un dígito,
        /// cuando se valida, entonces no es válido.
        /// </summary>
        [TestCase("AAA123456X")]
        [TestCase("AAA123456<")]
        public void EsValido_UltimoCaracterNoNumerico_DevuelveFalse(string pasaporte) {
            // Arrange

            // Act
            var resultado = PasaporteValidator.EsValido(pasaporte);

            // Assert
            resultado.Should().BeFalse();
        }

        /// <summary>
        /// Dado un pasaporte con símbolos no permitidos en el número,
        /// cuando se valida, entonces no es válido.
        /// </summary>
        [TestCase("AA@1234565")]
        [TestCase("AAA12-4565")]
        public void EsValido_CaracterNoPermitido_DevuelveFalse(string pasaporte) {
            // Arrange

            // Act
            var resultado = PasaporteValidator.EsValido(pasaporte);

            // Assert
            resultado.Should().BeFalse();
        }

        /// <summary>
        /// Dada una cadena vacía o con caracteres no permitidos,
        /// cuando se calcula su dígito de control, entonces no se puede calcular y devuelve nulo.
        /// </summary>
        [TestCase("")]
        [TestCase("AB@1")]
        [TestCase("ab1")]
        public void CalcularDigitoControl_CadenaNoCalculable_DevuelveNull(string campo) {
            // Arrange

            // Act
            var digito = PasaporteValidator.CalcularDigitoControl(campo);

            // Assert
            digito.Should().BeNull();
        }

        /// <summary>
        /// Dada una cadena nula, cuando se calcula su dígito de control,
        /// entonces no se puede calcular y devuelve nulo.
        /// </summary>
        [Test]
        public void CalcularDigitoControl_Nulo_DevuelveNull() {
            // Arrange

            // Act
            var digito = PasaporteValidator.CalcularDigitoControl(null);

            // Assert
            digito.Should().BeNull();
        }
    }
}