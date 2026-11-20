using System.Reflection;
using System.Runtime.CompilerServices;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Models;

/// <summary>
/// Tests unitarios de la entidad <see cref="Vehiculo"/>.
/// Cubre valores por defecto, asignación completa, igualdad por referencia,
/// inmutabilidad de las propiedades de negocio y mutabilidad de los campos de auditoría.
/// </summary>
public class VehiculoTests
{
    private static readonly string[] PropiedadesDeAuditoria =
    [
        nameof(Vehiculo.CreatedAt),
        nameof(Vehiculo.UpdatedAt),
        nameof(Vehiculo.DeletedAt)
    ];

    private static Vehiculo CrearVehiculoCompleto() => new()
    {
        Id = "VH-001",
        FotoFk = "foto-001",
        Matrícula = "1234BCD",
        Marca = "Toyota",
        Modelo = "Corolla",
        Acriss = "CDMR",
        Carroceria = TipoCarroceria.Sedan,
        Combustible = TipoCombustible.Hibrido,
        Transmision = TipoTransmision.Automatica,
        Estado = EstadoVehiculo.EnAlquiler,
        Color = TipoColor.Azul,
        PrecioBaseDia = 45.90,
        Plazas = 5,
        MaleteroLitros = 361,
        AutonomiaKm = 780,
        Kilometraje = 12_500,
        FechaUltimaRevision = new DateTime(2026, 10, 1),
        ConesionarioFk = 10L,
        Activo = true,
        CreatedAt = new DateTime(2025, 1, 1),
        UpdatedAt = new DateTime(2025, 6, 1),
        DeletedAt = null
    };

    #region Valores por defecto

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
    public void Constructor_PorDefecto_CreatedAtEsLaFechaActual()
    {
        // Arrange
        var antes = DateTime.UtcNow;

        // Act
        var vehiculo = new Vehiculo();

        // Assert
        vehiculo.CreatedAt.Should().BeCloseTo(antes, TimeSpan.FromMinutes(1));
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
            FotoFk = string.Empty,
            Foto = (Foto?)null,
            Matrícula = string.Empty,
            Marca = string.Empty,
            Modelo = string.Empty,
            Acriss = string.Empty,
            Carroceria = TipoCarroceria.Suv,
            Combustible = TipoCombustible.Gasolina,
            Transmision = TipoTransmision.Manual,
            Color = TipoColor.Rojo,
            PrecioBaseDia = 0d,
            Plazas = 0,
            MaleteroLitros = 0,
            AutonomiaKm = 0,
            Kilometraje = 0,
            FechaUltimaRevision = default(DateTime),
            ConesionarioFk = 0L,
            UpdatedAt = (DateTime?)null,
            DeletedAt = (DateTime?)null
        });
    }

    #endregion

    #region Asignación completa

    [Test]
    public void Constructor_ConTodosLosValores_AsignaCadaPropiedad()
    {
        // Arrange & Act
        var vehiculo = CrearVehiculoCompleto();

        // Assert
        vehiculo.Should().BeEquivalentTo(new
        {
            Id = "VH-001",
            FotoFk = "foto-001",
            Matrícula = "1234BCD",
            Marca = "Toyota",
            Modelo = "Corolla",
            Acriss = "CDMR",
            Carroceria = TipoCarroceria.Sedan,
            Combustible = TipoCombustible.Hibrido,
            Transmision = TipoTransmision.Automatica,
            Estado = EstadoVehiculo.EnAlquiler,
            Color = TipoColor.Azul,
            PrecioBaseDia = 45.90,
            Plazas = 5,
            MaleteroLitros = 361,
            AutonomiaKm = 780,
            Kilometraje = 12_500,
            FechaUltimaRevision = new DateTime(2026, 10, 1),
            ConesionarioFk = 10L,
            Activo = true,
            CreatedAt = new DateTime(2025, 1, 1),
            UpdatedAt = new DateTime(2025, 6, 1),
            DeletedAt = (DateTime?)null
        });
    }

    [Test]
    public void Constructor_ConFoto_ConservaLaReferencia()
    {
        // Arrange
        var foto = new Foto { Nombre = "delantera.jpg", Ruta = "/fotos/delantera.jpg" };

        // Act
        var vehiculo = new Vehiculo { Foto = foto };

        // Assert
        vehiculo.Foto.Should().BeSameAs(foto);
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

    #endregion

    #region Igualdad

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

    #endregion

    #region Inmutabilidad y contrato de propiedades

    [Test]
    public void Propiedades_DeclaradasInitOnly_NoPermitenMutacionTrasConstruccion()
    {
        // Arrange
        var propiedades = typeof(Vehiculo)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(propiedad => propiedad.SetMethod is not null)
            .Where(propiedad => !PropiedadesDeAuditoria.Contains(propiedad.Name));

        // Act & Assert
        propiedades.Should().OnlyContain(propiedad =>
            propiedad.SetMethod!.ReturnParameter
                .GetRequiredCustomModifiers()
                .Contains(typeof(IsExternalInit)));
    }

    [Test]
    public void PropiedadesDeAuditoria_TienenSetter_SePuedenModificar()
    {
        // Arrange
        var vehiculo = new Vehiculo();
        var fecha = new DateTime(2026, 11, 18);

        // Act
        vehiculo.CreatedAt = fecha;
        vehiculo.UpdatedAt = fecha;
        vehiculo.DeletedAt = fecha;

        // Assert
        (vehiculo.CreatedAt, vehiculo.UpdatedAt, vehiculo.DeletedAt)
            .Should().Be((fecha, fecha, fecha));
    }

    [Test]
    public void Propiedades_Publicas_ExponenLasVeintitresDelModelo()
    {
        // Arrange
        var nombresEsperados = new[]
        {
            nameof(Vehiculo.Id),
            nameof(Vehiculo.FotoFk),
            nameof(Vehiculo.Foto),
            nameof(Vehiculo.Matrícula),
            nameof(Vehiculo.Marca),
            nameof(Vehiculo.Modelo),
            nameof(Vehiculo.Acriss),
            nameof(Vehiculo.Carroceria),
            nameof(Vehiculo.Combustible),
            nameof(Vehiculo.Transmision),
            nameof(Vehiculo.Estado),
            nameof(Vehiculo.Color),
            nameof(Vehiculo.PrecioBaseDia),
            nameof(Vehiculo.Plazas),
            nameof(Vehiculo.MaleteroLitros),
            nameof(Vehiculo.AutonomiaKm),
            nameof(Vehiculo.Kilometraje),
            nameof(Vehiculo.FechaUltimaRevision),
            nameof(Vehiculo.ConesionarioFk),
            nameof(Vehiculo.Activo),
            nameof(Vehiculo.CreatedAt),
            nameof(Vehiculo.UpdatedAt),
            nameof(Vehiculo.DeletedAt)
        };

        // Act
        var nombres = typeof(Vehiculo)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(propiedad => propiedad.Name);

        // Assert
        nombres.Should().BeEquivalentTo(nombresEsperados);
    }

    #endregion
}
