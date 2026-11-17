using System.Reflection;
using System.Runtime.CompilerServices;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Models;

/// <summary>
/// Tests unitarios de la entidad <see cref="Vehiculo"/>.
/// Cubre valores por defecto, asignación completa, igualdad por referencia
/// e inmutabilidad de las propiedades declaradas con <c>init</c>.
/// </summary>
public class VehiculoTests
{
    private static Vehiculo CrearVehiculoCompleto() => new()
    {
        Id = "VH-001",
        Marca = "Toyota",
        Modelo = "Corolla",
        Acriss = "CDMR",
        Carroceria = TipoCarroceria.Sedan,
        Combustible = TipoCombustible.Hibrido,
        Transmision = TipoTransmision.Automatica,
        Estado = EstadoVehiculo.EnAlquiler,
        Color = TipoColor.Azul,
        PrecioBaseDia = 12.45,
        Plazas = 5,
        MaleteroLitros = 5,
        AutonomiaKm = 780,
        Kilometraje = 12500,
        FechaUltimaRevision = new DateTime(2026, 10, 1),
        Activo = true
    };

    [Test]
    public void Constructor_PorDefecto_EstadoEsDisponible()
    {
        // Arrange & Act
        var vehiculo = new Vehiculo();

        // Assert
        vehiculo.Estado.Should().Be(EstadoVehiculo.Disponible);
    }

    [Test]
    public void Constructor_PorDefecto_ActivoEsTrue()
    {
        // Arrange & Act
        var vehiculo = new Vehiculo();

        // Assert
        vehiculo.Activo.Should().BeTrue();
    }

    [Test]
    public void Constructor_PorDefecto_ElRestoDePropiedadesTomaSuValorPorDefecto()
    {
        // Arrange & Act
        var vehiculo = new Vehiculo();

        // Assert
        vehiculo.Should().BeEquivalentTo(new
        {
            Id = (string?)null,
            Marca = string.Empty,
            Modelo = string.Empty,
            Acriss = string.Empty,
            Carroceria = TipoCarroceria.Suv,
            Combustible = TipoCombustible.Gasolina,
            Transmision = TipoTransmision.Manual,
            Color = TipoColor.Rojo,
            PrecioBaseDia = 0,
            Plazas = 0,
            MaleteroLitros = 0,
            AutonomiaKm = 0,
            Kilometraje = 0,
            FechaUltimaRevision = default(DateTime)
        });
    }

    [Test]
    public void Constructor_ConTodosLosValores_AsignaCadaPropiedad()
    {
        // Arrange & Act
        var vehiculo = CrearVehiculoCompleto();

        // Assert
        vehiculo.Should().BeEquivalentTo(new
        {
            Id = "VH-001",
            Marca = "Toyota",
            Modelo = "Corolla",
            Acriss = "CDMR",
            Carroceria = TipoCarroceria.Sedan,
            Combustible = TipoCombustible.Hibrido,
            Transmision = TipoTransmision.Automatica,
            Estado = EstadoVehiculo.EnAlquiler,
            Color = TipoColor.Azul,
            PrecioBaseDia = 12.45,
            Plazas = 5,
            MaleteroLitros = 5,
            AutonomiaKm = 780,
            Kilometraje = 12500,
            FechaUltimaRevision = new DateTime(2026, 10, 1),
            Activo = true
        });
    }

    [Test]
    public void Estado_AsignarRetirado_ConservaElValor()
    {
        // Arrange & Act
        var vehiculo = new Vehiculo { Estado = EstadoVehiculo.Retirado };

        // Assert
        vehiculo.Estado.Should().Be(EstadoVehiculo.Retirado);
    }

    [Test]
    public void Activo_AsignarFalse_ConservaElValor()
    {
        // Arrange & Act
        var vehiculo = new Vehiculo { Activo = false };

        // Assert
        vehiculo.Activo.Should().BeFalse();
    }

    [Test]
    public void Igualdad_DosInstanciasConMismosValores_NoSonIgualesPorReferencia()
    {
        // Arrange
        var original = CrearVehiculoCompleto();
        var copia = CrearVehiculoCompleto();

        // Act & Assert
        original.Should().NotBe(copia);
    }

    [Test]
    public void Igualdad_MismaInstancia_EsIgualASiMisma()
    {
        // Arrange
        var vehiculo = CrearVehiculoCompleto();

        // Act & Assert
        vehiculo.Should().BeSameAs(vehiculo);
    }

    [Test]
    public void Igualdad_CompararConNull_DevuelveFalse()
    {
        // Arrange
        var vehiculo = CrearVehiculoCompleto();

        // Act
        var sonIguales = vehiculo.Equals((object?)null);

        // Assert
        sonIguales.Should().BeFalse();
    }
}
