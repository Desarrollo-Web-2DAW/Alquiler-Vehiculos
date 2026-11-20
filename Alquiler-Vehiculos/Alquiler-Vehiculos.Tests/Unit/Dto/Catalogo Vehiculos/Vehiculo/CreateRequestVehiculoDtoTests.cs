using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Dto.Catalogo_Vehiculos.Vehiculo;

/// <summary>
/// Tests unitarios de <see cref="CreateRequestVehiculoDto"/>.
/// </summary>
public class CreateRequestVehiculoDtoTests
{
    [Test]
    public void Constructor_SinDatos_InicializaValoresPorDefecto()
    {
        // Arrange & Act
        var dto = new CreateRequestVehiculoDto();

        // Assert
        dto.Should().BeEquivalentTo(new
        {
            Matricula = string.Empty,
            Acriss = string.Empty,
            Categoria = string.Empty,
            PrecioBaseDia = 0d,
            Color = string.Empty,
            Estado = string.Empty,
            Combustible = string.Empty,
            Transmision = string.Empty,
            Plazas = 0,
            MaleteroLitros = 0,
            AutonomiaKm = 0,
            Kilometraje = 0,
            FechaUltimaRevision = string.Empty,
            IsActivo = false
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Propiedades_AsignarDatos_ConservaValores(bool isActivo)
    {
        // Arrange
        var dto = new CreateRequestVehiculoDto
        {
            Matricula = "1234BCD",
            Acriss = "CDMR",
            Categoria = "Compacto",
            PrecioBaseDia = 45.50,
            Color = "Azul",
            Estado = "Disponible",
            Combustible = "Gasolina",
            Transmision = "Manual",
            Plazas = 5,
            MaleteroLitros = 361,
            AutonomiaKm = 700,
            Kilometraje = 12_000,
            FechaUltimaRevision = "2026-11-01",
            IsActivo = isActivo
        };

        // Act & Assert
        dto.Should().BeEquivalentTo(new
        {
            Matricula = "1234BCD",
            Acriss = "CDMR",
            Categoria = "Compacto",
            PrecioBaseDia = 45.50,
            Color = "Azul",
            Estado = "Disponible",
            Combustible = "Gasolina",
            Transmision = "Manual",
            Plazas = 5,
            MaleteroLitros = 361,
            AutonomiaKm = 700,
            Kilometraje = 12_000,
            FechaUltimaRevision = "2026-11-01",
            IsActivo = isActivo
        });
    }
}
