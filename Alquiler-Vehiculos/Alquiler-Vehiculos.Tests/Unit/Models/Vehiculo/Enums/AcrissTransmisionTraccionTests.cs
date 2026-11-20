using Alquiler_Vehiculos.Models.Vehiculo.Enums.ACRISS;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Enums;

/// <summary>
/// Tests unitarios del enum <see cref="AcrissTransmisionTraccion"/> (tercera letra del código ACRISS).
/// </summary>
[TestFixture]
[Category("Unit")]
public class AcrissTransmisionTraccionTests
{
    [TestCase(AcrissTransmisionTraccion.ManualSinTraccionDefinida, 'M')]
    [TestCase(AcrissTransmisionTraccion.Manual4Wd, 'N')]
    [TestCase(AcrissTransmisionTraccion.ManualAwd, 'C')]
    [TestCase(AcrissTransmisionTraccion.AutomaticaSinTraccionDefinida, 'A')]
    [TestCase(AcrissTransmisionTraccion.Automatica4Wd, 'B')]
    [TestCase(AcrissTransmisionTraccion.AutomaticaAwd, 'D')]
    [TestCase(AcrissTransmisionTraccion.AutomatizacionNivel3, 'Q')]
    [TestCase(AcrissTransmisionTraccion.AutomatizacionNivel4, 'H')]
    [TestCase(AcrissTransmisionTraccion.AutomatizacionNivel5, 'F')]
    public void Valor_CasteadoAChar_DevuelveSuCodigoAcriss(
        AcrissTransmisionTraccion valor,
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
        var valores = Enum.GetValues<AcrissTransmisionTraccion>();

        // Act
        var convertidos = valores.Select(valor => (AcrissTransmisionTraccion)(char)valor).ToArray();

        // Assert
        convertidos.Should().Equal(valores);
    }

    [Test]
    public void Valores_NoTienenDuplicados()
    {
        // Arrange
        var valores = Enum.GetValues<AcrissTransmisionTraccion>();

        // Act
        var distintos = valores.Distinct().ToArray();

        // Assert
        distintos.Should().HaveCount(valores.Length);
    }

    [Test]
    public void Valores_SonLetrasMayusculasAscii()
    {
        // Arrange
        var valores = Enum.GetValues<AcrissTransmisionTraccion>();

        // Act
        var codigos = valores.Select(valor => (char)valor).ToArray();

        // Assert
        codigos.Should().OnlyContain(codigo => codigo >= 'A' && codigo <= 'Z');
    }

    [Test]
    public void Valor_NoDefinido_NoEsReconocidoPorElEnum()
    {
        // Arrange
        const char codigoInexistente = 'E';

        // Act
        var estaDefinido = Enum.IsDefined(typeof(AcrissTransmisionTraccion), (int)codigoInexistente);

        // Assert
        estaDefinido.Should().BeFalse();
    }
}
