using Alquiler_Vehiculos.Models.Vehiculo.Enums.ACRISS;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Enums;

/// <summary>
/// Tests unitarios del enum <see cref="AcrissCombustibleClimatizacion"/> (cuarta letra del código ACRISS).
/// </summary>
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
    [TestCase(AcrissCombustibleClimatizacion.MulticomustibleConAire, 'M')]
    [TestCase(AcrissCombustibleClimatizacion.EtanolConAire, 'F')]
    [TestCase(AcrissCombustibleClimatizacion.GasolinaConAire, 'V')]
    [TestCase(AcrissCombustibleClimatizacion.GasolinaSinAire, 'Z')]
    [TestCase(AcrissCombustibleClimatizacion.EtanolSinAire, 'X')]
    public void Valor_CasteadoAChar_DevuelveSuCodigoAcriss(
        AcrissCombustibleClimatizacion valor,
        char codigoEsperado)
    {
        // Arrange & Act
        var codigo = (char)valor;

        // Assert
        codigo.Should().Be(codigoEsperado);
    }

    [Test]
    public void Codigo_CasteadoAlEnum_DevuelveSuValor()
    {
        // Arrange
        var pares = Enum.GetValues<AcrissCombustibleClimatizacion>()
            .Select(valor => (Codigo: (char)valor, Valor: valor));

        // Act & Assert
        pares.Should().OnlyContain(par => (AcrissCombustibleClimatizacion)par.Codigo == par.Valor);
    }

    [Test]
    public void Valores_NoTienenDuplicados()
    {
        // Arrange
        var valores = Enum.GetValues<AcrissCombustibleClimatizacion>();

        // Act & Assert
        valores.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void Valores_SonLetrasMayusculasAscii()
    {
        // Arrange
        var codigos = Enum.GetValues<AcrissCombustibleClimatizacion>().Select(valor => (char)valor);

        // Act & Assert
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
