using Alquiler_Vehiculos.Models.Vehiculo.Enums.ACRISS;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Enums;

/// <summary>
/// Tests unitarios del enum <see cref="AcrissCategoria"/> (primera letra del código ACRISS).
/// El contrato del enum es que cada constante vale exactamente el código ASCII de su letra,
/// para poder convertir en ambos sentidos con <c>(char)valor</c> y <c>(AcrissCategoria)'X'</c>.
/// </summary>
[TestFixture]
[Category("Unit")]
public class AcrissCategoriaTests
{
    [TestCase(AcrissCategoria.Mini, 'M')]
    [TestCase(AcrissCategoria.MiniElite, 'N')]
    [TestCase(AcrissCategoria.Economico, 'E')]
    [TestCase(AcrissCategoria.EconomicoElite, 'H')]
    [TestCase(AcrissCategoria.Compacto, 'C')]
    [TestCase(AcrissCategoria.CompactoElite, 'D')]
    [TestCase(AcrissCategoria.Intermedio, 'I')]
    [TestCase(AcrissCategoria.IntermedioElite, 'J')]
    [TestCase(AcrissCategoria.Estandar, 'S')]
    [TestCase(AcrissCategoria.EstandarElite, 'R')]
    [TestCase(AcrissCategoria.Grande, 'F')]
    [TestCase(AcrissCategoria.GrandeElite, 'G')]
    [TestCase(AcrissCategoria.Premium, 'P')]
    [TestCase(AcrissCategoria.PremiumElite, 'U')]
    [TestCase(AcrissCategoria.Lujo, 'L')]
    [TestCase(AcrissCategoria.LujoElite, 'W')]
    [TestCase(AcrissCategoria.Sobredimensionado, 'O')]
    [TestCase(AcrissCategoria.Especial, 'X')]
    public void Valor_CasteadoAChar_DevuelveSuCodigoAcriss(
        AcrissCategoria valor,
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
        var valores = Enum.GetValues<AcrissCategoria>();

        // Act
        var convertidos = valores.Select(valor => (AcrissCategoria)(char)valor).ToArray();

        // Assert
        convertidos.Should().Equal(valores);
    }

    [Test]
    public void Valores_NoTienenDuplicados()
    {
        // Arrange
        var valores = Enum.GetValues<AcrissCategoria>();

        // Act
        var distintos = valores.Distinct().ToArray();

        // Assert
        distintos.Should().HaveCount(valores.Length);
    }

    [Test]
    public void Valores_SonLetrasMayusculasAscii()
    {
        // Arrange
        var valores = Enum.GetValues<AcrissCategoria>();

        // Act
        var codigos = valores.Select(valor => (char)valor).ToArray();

        // Assert
        codigos.Should().OnlyContain(codigo => codigo >= 'A' && codigo <= 'Z');
    }

    [Test]
    public void Valor_NoDefinido_NoEsReconocidoPorElEnum()
    {
        // Arrange
        const char codigoInexistente = 'A';

        // Act
        var estaDefinido = Enum.IsDefined(typeof(AcrissCategoria), (int)codigoInexistente);

        // Assert
        estaDefinido.Should().BeFalse();
    }
}
