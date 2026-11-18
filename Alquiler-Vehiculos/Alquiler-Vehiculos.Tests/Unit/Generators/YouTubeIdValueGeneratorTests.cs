using Alquiler_Vehiculos.Generators;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Generators;

[TestFixture]
[Category("Unit")]
public class YouTubeIdValueGeneratorTests {
    [Test]
    public void Next_SinUsarEntrada_GeneraOnceCaracteresDelAlfabetoPermitido() {
        // Arrange
        var generator = new YouTubeIdValueGenerator();

        // Act
        var id = generator.Next(null!);

        // Assert
        id.Should().MatchRegex("^[A-Za-z0-9_-]{11}$");
    }

    [Test]
    public void GeneratesTemporaryValues_AlConsultar_DevuelveFalse() {
        // Arrange
        var generator = new YouTubeIdValueGenerator();

        // Act
        var temporal = generator.GeneratesTemporaryValues;

        // Assert
        temporal.Should().BeFalse();
    }
}
