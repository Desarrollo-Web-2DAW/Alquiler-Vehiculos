using Alquiler_Vehiculos.Validators.Trabajadores;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validator;

[TestFixture]
[Category("Unit")]
public class TrabajadorValidatorTests {
    // ── Casos positivos ──────────────────────────────────────────────────
    [TestCase("12345678Z")]   // Ejemplo válido RF-16 (resto 14 -> Z)
    [TestCase("00000000T")]   // Resto 0 -> T
    [TestCase("00000022E")]   // Resto 22 -> E
    [TestCase("11111111H")]   // DNI válido de prueba (resto 18 -> H)
    [TestCase(" 12345678z ")] // Minúscula y espacios alrededor
    public void ValidarLetraDni_DniValido_DevuelveTrue(string dni) {
        // Arrange
        // El DNI de entrada llega parametrizado en el TestCase
        // Act
        var resultado = TrabajadorValidator.ValidarLetraDni(dni);
        // Assert
        resultado.Should().BeTrue();
    }
    // ── Casos negativos ──────────────────────────────────────────────────
    [TestCase("12345678A")]   // Ejemplo inválido RF-16 (letra incorrecta)
    [TestCase("12345678I")]   // Letra excluida de la serie oficial
    [TestCase("12345678O")]   // Letra excluida de la serie oficial
    [TestCase("1234567Z")]    // Menos de 8 dígitos
    [TestCase("123456789Z")]  // Más de 8 dígitos
    [TestCase("1234567AZ")]   // Letra en la parte numérica
    [TestCase("123456789")]   // Sin letra final
    [TestCase("")]            // Cadena vacía
    [TestCase("   ")]         // Solo espacios
    public void ValidarLetraDni_DniInvalido_DevuelveFalse(string dni) {
        // Arrange
        // El DNI de entrada llega parametrizado en el TestCase
        // Act
        var resultado = TrabajadorValidator.ValidarLetraDni(dni);
        // Assert
        resultado.Should().BeFalse();
    }
}