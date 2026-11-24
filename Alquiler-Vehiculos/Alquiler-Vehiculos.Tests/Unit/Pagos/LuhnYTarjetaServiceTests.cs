using Alquiler_Vehiculos.Services.Pago;
using Alquiler_Vehiculos.Validators.Pagos;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Pagos;

/// <summary>
/// Pruebas unitarias de Luhn (RF-21) y cifrado AES-256-GCM de tarjetas (RF-22).
/// </summary>
[TestFixture]
[Category("Unit")]
public class LuhnYTarjetaServiceTests {
    private static readonly DateTime Ahora = new(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc);
    private LuhnValidator _validator = null!;
    private TarjetaCryptoService _crypto = null!;

    [SetUp]
    public void SetUp() {
        _validator = new LuhnValidator(new RelojFijo(new DateTimeOffset(Ahora)));
        _crypto = new TarjetaCryptoService("clave-cifrado-tarjetas-solo-tests-minimo-32-bytes!");
    }

    [TestCase("79927398713", true)]   // Ejemplo válido literal de RF-21
    [TestCase("79927398714", false)]  // Ejemplo inválido literal de RF-21 (falla por 1 dígito)
    [TestCase("4532015112830366", true)]
    [TestCase("4532015112830367", false)]
    [TestCase("5425 2334 3010 9903", true)]
    public void EsLuhnValido_CasosConocidos_DevuelveResultadoEsperado(string numero, bool esperado) {
        // Act & Assert
        LuhnValidator.EsLuhnValido(numero).Should().Be(esperado);
    }

    [TestCase("Debito")]
    [TestCase("Transferencia")]
    public async Task ValidateAsync_MedioDistintoDeCredito_Falla(string medio) {
        // Arrange
        var dto = new DatosTarjetaDto(medio, "4532015112830366", 12, 2028, "123");

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(DatosTarjetaDto.TipoMedio));
    }

    [Test]
    public async Task ValidateAsync_TarjetaCaducada_Falla() {
        // Arrange
        var dto = new DatosTarjetaDto("Credito", "4532015112830366", 11, 2026, "123");

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "caducidad");
    }

    [Test]
    public void CifrarYDescifrar_NumeroTarjeta_NoExponeEnClaroYUsaNonceAleatorio() {
        // Arrange
        const string pan = "4532 0151 1283 0366";

        // Act
        var cifrado1 = _crypto.CifrarNumero(pan);
        var cifrado2 = _crypto.CifrarNumero(pan);
        var descifrado = _crypto.DescifrarNumero(cifrado1);

        // Assert
        cifrado1.Should().NotContain("4532").And.NotContain("0366");
        cifrado1.Should().NotBe(cifrado2, "cada cifrado debe usar un nonce aleatorio");
        descifrado.Should().Be("4532015112830366");
        _crypto.ObtenerUltimosCuatro(pan).Should().Be("0366");
        _crypto.DetectarMarca(pan).Should().Be("Visa");
    }

    [Test]
    public void Constructor_ClaveCorta_LanzaInvalidOperationException() {
        // Act
        var act = () => new TarjetaCryptoService("corta");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
