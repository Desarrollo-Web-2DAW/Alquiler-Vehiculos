using Alquiler_Vehiculos.Models.Vehiculo.Enums.ACRISS;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Enums;

/// <summary>
/// Tests unitarios del enum <see cref="AcrissTipo"/> (segunda letra del código ACRISS).
/// </summary>
public class AcrissTipoTests
{
    [TestCase(AcrissTipo.DosTresPuertas, 'B')]
    [TestCase(AcrissTipo.DosCuatroPuertas, 'C')]
    [TestCase(AcrissTipo.CuatroCincoPuertas, 'D')]
    [TestCase(AcrissTipo.Familiar, 'W')]
    [TestCase(AcrissTipo.FurgonetaPasajeros, 'V')]
    [TestCase(AcrissTipo.Berlina, 'L')]
    [TestCase(AcrissTipo.Deportivo, 'S')]
    [TestCase(AcrissTipo.Descapotable, 'T')]
    [TestCase(AcrissTipo.Suv, 'F')]
    [TestCase(AcrissTipo.TodoTerrenoAbierto, 'J')]
    [TestCase(AcrissTipo.Especial, 'X')]
    [TestCase(AcrissTipo.PickupCabinaSimple, 'P')]
    [TestCase(AcrissTipo.PickupDobleCabina, 'Q')]
    [TestCase(AcrissTipo.OfertaEspecial, 'Z')]
    [TestCase(AcrissTipo.Coupe, 'E')]
    [TestCase(AcrissTipo.Monovolumen, 'M')]
    [TestCase(AcrissTipo.Recreativo, 'R')]
    [TestCase(AcrissTipo.Autocaravana, 'H')]
    [TestCase(AcrissTipo.DosRuedas, 'Y')]
    [TestCase(AcrissTipo.Roadster, 'N')]
    [TestCase(AcrissTipo.Crossover, 'G')]
    [TestCase(AcrissTipo.FurgonetaComercial, 'K')]
    public void Valor_CasteadoAChar_DevuelveSuCodigoAcriss(
        AcrissTipo valor,
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
        var pares = Enum.GetValues<AcrissTipo>()
            .Select(valor => (Codigo: (char)valor, Valor: valor));

        // Act & Assert
        pares.Should().OnlyContain(par => (AcrissTipo)par.Codigo == par.Valor);
    }

    [Test]
    public void Valores_NoTienenDuplicados()
    {
        // Arrange
        var valores = Enum.GetValues<AcrissTipo>();

        // Act & Assert
        valores.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void Valores_SonLetrasMayusculasAscii()
    {
        // Arrange
        var codigos = Enum.GetValues<AcrissTipo>().Select(valor => (char)valor);

        // Act & Assert
        codigos.Should().OnlyContain(codigo => codigo >= 'A' && codigo <= 'Z');
    }

    [Test]
    public void Valor_NoDefinido_NoEsReconocidoPorElEnum()
    {
        // Arrange
        const char codigoInexistente = 'A';

        // Act
        var estaDefinido = Enum.IsDefined(typeof(AcrissTipo), (int)codigoInexistente);

        // Assert
        estaDefinido.Should().BeFalse();
    }
}
