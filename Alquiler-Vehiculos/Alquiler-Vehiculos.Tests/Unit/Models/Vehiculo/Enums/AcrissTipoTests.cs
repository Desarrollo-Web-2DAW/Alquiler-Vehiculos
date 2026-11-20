using Alquiler_Vehiculos.Models.Vehiculo.Enums.ACRISS;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Models.Vehiculo.Enums;

/// <summary>
/// Tests unitarios del enum <see cref="AcrissTipo"/> (segunda letra del código ACRISS).
/// </summary>
[TestFixture]
[Category("Unit")]
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
    [TestCase(AcrissTipo.Roadster, 'N')]
    [TestCase(AcrissTipo.Crossover, 'G')]
    [TestCase(AcrissTipo.FurgonetaComercial, 'K')]
    public void Valor_CasteadoAChar_DevuelveSuCodigoAcriss(
        AcrissTipo valor,
        char codigoEsperado)
    {
        // Arrange
        // El valor y el código esperado se reciben del caso de prueba.

        // Act
        var codigo = (char)valor;

        // Assert
        codigo.Should().Be(codigoEsperado);
    }

    [Test]
    public void Codigo_CasteadoAlEnum_DevuelveSuValor()
    {
        // Arrange
        var valores = Enum.GetValues<AcrissTipo>();

        // Act
        var convertidos = valores.Select(valor => (AcrissTipo)(char)valor).ToArray();

        // Assert
        convertidos.Should().Equal(valores);
    }

    [Test]
    public void Valores_NoTienenDuplicados()
    {
        // Arrange
        var valores = Enum.GetValues<AcrissTipo>();

        // Act
        var distintos = valores.Distinct().ToArray();

        // Assert
        distintos.Should().HaveCount(valores.Length);
    }

    [Test]
    public void Valores_SonLetrasMayusculasAscii()
    {
        // Arrange
        var valores = Enum.GetValues<AcrissTipo>();

        // Act
        var codigos = valores.Select(valor => (char)valor).ToArray();

        // Assert
        codigos.Should().OnlyContain(codigo => codigo >= 'A' && codigo <= 'Z');
    }

    [TestCase('A')]
    [TestCase('Y')]
    public void Valor_NoDefinido_NoEsReconocidoPorElEnum(char codigoInexistente)
    {
        // Arrange
        // La flota no admite el tipo de dos ruedas ('Y'), según RF-03.

        // Act
        var estaDefinido = Enum.IsDefined(typeof(AcrissTipo), (int)codigoInexistente);

        // Assert
        estaDefinido.Should().BeFalse();
    }
}
