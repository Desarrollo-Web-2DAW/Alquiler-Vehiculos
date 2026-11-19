using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Models;

/// <summary>
/// Tests unitarios de la entidad <see cref="VehiculoModel"/>.
/// Cubre valores por defecto, asignación completa, igualdad por referencia
/// y auditoría.
/// </summary>
[TestFixture]
[Category("Unit")]
public class VehiculoModelTests
{
    private static VehiculoModel CrearVehiculoCompleto() => new()
    {
        Id = "AbCdEf123_-",
        Marca = "Toyota",
        Modelo = "Corolla",
        Acriss = "ILAH",
        Carroceria = TipoCarroceria.Sedan,
        Combustible = TipoCombustible.Hibrido,
        Transmision = TipoTransmision.Automatica,
        Estado = EstadoVehiculo.EnAlquiler,
        Color = TipoColor.Azul,
        PrecioBaseDia = 12.45m,
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
        // Arrange
        // No se necesitan datos previos.

        // Act
        var vehiculo = new VehiculoModel();

        // Assert
        vehiculo.Estado.Should().Be(EstadoVehiculo.Disponible);
    }

    [Test]
    public void Constructor_PorDefecto_ActivoEsTrue()
    {
        // Arrange
        // No se necesitan datos previos.

        // Act
        var vehiculo = new VehiculoModel();

        // Assert
        vehiculo.Activo.Should().BeTrue();
    }

    [Test]
    public void Constructor_PorDefecto_ElRestoDePropiedadesTomaSuValorPorDefecto()
    {
        // Arrange
        // No se necesitan datos previos.

        // Act
        var vehiculo = new VehiculoModel();

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
            PrecioBaseDia = 0m,
            Plazas = 0,
            MaleteroLitros = 0,
            AutonomiaKm = 0,
            Kilometraje = 0,
            FechaUltimaRevision = default(DateTime)
        });
    }

    [Test]
    public void Constructor_PorDefecto_InicializaFotoMatriculaConcesionarioYAuditoria()
    {
        // Arrange
        var antes = DateTime.UtcNow;

        // Act
        var vehiculo = new VehiculoModel();

        // Assert
        vehiculo.FotoFk.Should().BeEmpty();
        vehiculo.Foto.Should().BeNull();
        vehiculo.Matrícula.Should().BeEmpty();
        vehiculo.ConesionarioFk.Should().Be(0L);
        vehiculo.CreatedAt.Should().BeOnOrAfter(antes).And.BeOnOrBefore(DateTime.UtcNow);
        vehiculo.UpdatedAt.Should().BeNull();
        vehiculo.DeletedAt.Should().BeNull();
    }

    [Test]
    public void Constructor_ConFotoMatriculaConcesionarioYAuditoria_ConservaValores()
    {
        // Arrange
        var fecha = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var foto = new Foto { Nombre = "coche.png", Ruta = "/uploads/vehiculos/coche.png" };

        // Act
        var vehiculo = new VehiculoModel
        {
            FotoFk = "foto-001", Foto = foto, Matrícula = "1234BCD", ConesionarioFk = 10,
            CreatedAt = fecha, UpdatedAt = fecha.AddDays(1), DeletedAt = fecha.AddDays(2)
        };

        // Assert
        vehiculo.FotoFk.Should().Be("foto-001");
        vehiculo.Foto.Should().BeSameAs(foto);
        vehiculo.Matrícula.Should().Be("1234BCD");
        vehiculo.ConesionarioFk.Should().Be(10L);
        vehiculo.CreatedAt.Should().Be(fecha);
        vehiculo.UpdatedAt.Should().Be(fecha.AddDays(1));
        vehiculo.DeletedAt.Should().Be(fecha.AddDays(2));
    }

    [Test]
    public void Constructor_ConTodosLosValores_AsignaCadaPropiedad()
    {
        // Arrange
        // Los datos se preparan en el método auxiliar.

        // Act
        var vehiculo = CrearVehiculoCompleto();

        // Assert
        vehiculo.Should().BeEquivalentTo(new
        {
            Id = "AbCdEf123_-",
            Marca = "Toyota",
            Modelo = "Corolla",
            Acriss = "ILAH",
            Carroceria = TipoCarroceria.Sedan,
            Combustible = TipoCombustible.Hibrido,
            Transmision = TipoTransmision.Automatica,
            Estado = EstadoVehiculo.EnAlquiler,
            Color = TipoColor.Azul,
            PrecioBaseDia = 12.45m,
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
        // Arrange
        const EstadoVehiculo estado = EstadoVehiculo.Retirado;

        // Act
        var vehiculo = new VehiculoModel { Estado = estado };

        // Assert
        vehiculo.Estado.Should().Be(EstadoVehiculo.Retirado);
    }

    [Test]
    public void Activo_AsignarFalse_ConservaElValor()
    {
        // Arrange
        const bool activo = false;

        // Act
        var vehiculo = new VehiculoModel { Activo = activo };

        // Assert
        vehiculo.Activo.Should().BeFalse();
    }

    [Test]
    public void Igualdad_DosInstanciasConMismosValores_NoSonIgualesPorReferencia()
    {
        // Arrange
        var original = CrearVehiculoCompleto();
        var copia = CrearVehiculoCompleto();

        // Act
        var iguales = original.Equals(copia);

        // Assert
        iguales.Should().BeFalse();
    }

    [Test]
    public void Igualdad_MismaInstancia_EsIgualASiMisma()
    {
        // Arrange
        var vehiculo = CrearVehiculoCompleto();

        // Act
        var iguales = vehiculo.Equals(vehiculo);

        // Assert
        iguales.Should().BeTrue();
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
