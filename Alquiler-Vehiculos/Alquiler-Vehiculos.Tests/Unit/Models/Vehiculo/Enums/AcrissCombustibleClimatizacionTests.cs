using Alquiler_Vehiculos.Models.Vehiculo.Enums.ACRISS;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Models.Vehiculo.Enums;

/// <summary>
/// Tests unitarios del enum <see cref="AcrissCombustibleClimatizacion"/> (cuarta letra del código ACRISS).
/// </summary>
[TestFixture]
[Category("Unit")]
public class AcrissCombustibleClimatizacionTests
{
    [TestCase(AcrissCombustibleClimatizacion.SinEspecificarConAire, 'R')]
    [TestCase(AcrissCombustibleClimatizacion.SinEspecificarSinAire, 'N')]
    [TestCase(AcrissCombustibleClimatizacion.DieselConAire, 'D')]
    [TestCase(AcrissCombustibleClimatizacion.DieselSinAire, 'Q')]
    [TestCase(AcrissCombustibleClimatizacion.Hibrido, 'H')]
    [TestCase(AcrissCombustibleClimatizacion.HibridoEnchufable, 'I')]
    [TestCase(AcrissCombustibleClimatizacion.ElectricoE, 'E')]
    [TestCase(AcrissCombustibleClimatizacion.ElectricoC, 'C')]
    [TestCase(AcrissCombustibleClimatizacion.GlpConAire, 'L')]
    [TestCase(AcrissCombustibleClimatizacion.GlpSinAire, 'S')]
    [TestCase(AcrissCombustibleClimatizacion.HidrogenoConAire, 'A')]
    [TestCase(AcrissCombustibleClimatizacion.HidrogenoSinAire, 'B')]
    [TestCase(AcrissCombustibleClimatizacion.MulticombustibleConAire, 'M')]
    [TestCase(AcrissCombustibleClimatizacion.MulticombustibleSinAire, 'F')]
    [TestCase(AcrissCombustibleClimatizacion.EtanolConAire, 'U')]
    [TestCase(AcrissCombustibleClimatizacion.GasolinaConAire, 'V')]
    [TestCase(AcrissCombustibleClimatizacion.GasolinaSinAire, 'Z')]
    [TestCase(AcrissCombustibleClimatizacion.EtanolSinAire, 'X')]
    public void Valor_CasteadoAChar_DevuelveSuCodigoAcriss(
        AcrissCombustibleClimatizacion valor,
        char codigoEsperado)
    {
        // Arrange
        // El valor se recibe del caso de prueba.

        // Act
        var codigo = (char)valor;

        // Assert
        codigo.Should().Be(codigoEsperado);
    }

    [Test]
    public void Codigo_CasteadoAlEnum_DevuelveSuValor()
    {
        // Arrange
        var valores = Enum.GetValues<AcrissCombustibleClimatizacion>();

        // Act
        var convertidos = valores.Select(valor => (AcrissCombustibleClimatizacion)(char)valor).ToArray();

        // Assert
        convertidos.Should().Equal(valores);
    }

    [Test]
    public void Valores_NoTienenDuplicados()
    {
        // Arrange
        var valores = Enum.GetValues<AcrissCombustibleClimatizacion>();

        // Act
        var distintos = valores.Distinct().ToArray();

        // Assert
        distintos.Should().HaveCount(valores.Length);
    }

    [Test]
    public void Valores_SonLetrasMayusculasAscii()
    {
        // Arrange
        var valores = Enum.GetValues<AcrissCombustibleClimatizacion>();

        // Act
        var codigos = valores.Select(valor => (char)valor).ToArray();

        // Assert
        codigos.Should().OnlyContain(codigo => codigo >= 'A' && codigo <= 'Z');
    }

    [Test]
    public void Valor_NoDefinido_NoEsReconocidoPorElEnum()
    {
        // Arrange
        const char codigoInexistente = 'O';

        // Act
        var estaDefinido = Enum.IsDefined(typeof(AcrissCombustibleClimatizacion), (int)codigoInexistente);

        // Assert
        estaDefinido.Should().BeFalse();
    }
}
