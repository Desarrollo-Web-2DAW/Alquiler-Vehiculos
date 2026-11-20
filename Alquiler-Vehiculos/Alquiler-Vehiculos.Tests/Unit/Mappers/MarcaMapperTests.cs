using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Marca;
using Alquiler_Vehiculos.Mappers.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo;
using FluentAssertions;
using MongoDB.Bson;

namespace Alquiler_Vehiculos.Tests.Unit.Mappers;

/// <summary>
/// Tests unitarios de <see cref="MarcaMapper"/>.
/// Cubren la traducción DTO → modelo, la traducción modelo → DTO y las garantías
/// que añade el mapper (id opcional, auditoría y copia defensiva de la colección).
/// </summary>
[TestFixture]
[Category("Unit")]
public class MarcaMapperTests
{
    private static readonly ObjectId Id = ObjectId.Parse("000000000000000000000007");
    private static CreateMarcaDto CrearDto() => new()
    {
        Nombre = "Toyota",
        Pais = "Japón",
        Web = "https://www.toyota.es",
        ModelosDisponibles = ["Corolla", "Yaris"]
    };

    private static MarcaModel CrearModelo() =>
        new(Id, "Toyota", "Japón", "https://www.toyota.es", ["Corolla", "Yaris"],
            false, new DateTime(2025, 1, 1), null, null);

    [Test]
    public void ToMarca_ConDtoValido_CopiaLasPropiedadesDeNegocio()
    {
        // Arrange
        var dto = CrearDto();

        // Act
        var modelo = dto.ToMarca(Id);

        // Assert
        modelo.Should().BeEquivalentTo(new
        {
            Id,
            Nombre = "Toyota",
            Pais = "Japón",
            Web = "https://www.toyota.es",
            ModelosDisponibles = new[] { "Corolla", "Yaris" }
        });
    }

    [Test]
    public void ToMarca_SinId_DejaObjectIdVacioParaElDriver()
    {
        // Arrange
        var dto = CrearDto();

        // Act
        var modelo = dto.ToMarca();

        // Assert
        modelo.Id.Should().Be(ObjectId.Empty);
    }

    [Test]
    public void ToMarca_Siempre_MarcaElRegistroComoNoBorrado()
    {
        // Arrange
        var dto = CrearDto();

        // Act
        var modelo = dto.ToMarca();

        // Assert
        modelo.IsDeleted.Should().BeFalse();
    }

    [Test]
    public void ToMarca_CuandoSeCrea_RegistraLaFechaActualDeCreacion()
    {
        // Arrange
        var antes = DateTime.UtcNow;

        // Act
        var modelo = CrearDto().ToMarca();

        // Assert
        modelo.CreatedAt.Should().BeCloseTo(antes, TimeSpan.FromMinutes(1));
    }

    [Test]
    public void ToMarca_CuandoSeCrea_NoTieneFechaDeModificacionNiDeBorrado()
    {
        // Arrange
        var dto = CrearDto();

        // Act
        var modelo = dto.ToMarca();

        // Assert
        (modelo.UpdatedAt, modelo.DeletedAt).Should().Be((null, null));
    }

    [Test]
    public void ToMarca_CopiaLaColeccionDeModelos_SinCompartirLaDelDto()
    {
        // Arrange
        var dto = CrearDto();
        var modelo = dto.ToMarca();

        // Act
        dto.ModelosDisponibles.Add("Prius");

        // Assert
        modelo.ModelosDisponibles.Should().BeEquivalentTo(new[] { "Corolla", "Yaris" });
    }

    [Test]
    public void ToDto_ConModelo_DevuelveUnDtoConLosMismosDatos()
    {
        // Arrange
        var modelo = CrearModelo();

        // Act
        var dto = modelo.ToDto();

        // Assert
        dto.Should().BeEquivalentTo(new
        {
            Id = Id.ToString(),
            Nombre = "Toyota",
            Pais = "Japón",
            Web = "https://www.toyota.es",
            ModelosDisponibles = new[] { "Corolla", "Yaris" }
        });
    }

    [Test]
    public void ToDto_CopiaLaColeccionDeModelos_SinCompartirLaDelModelo()
    {
        // Arrange
        var modelo = CrearModelo();
        var dto = modelo.ToDto();

        // Act
        modelo.ModelosDisponibles.Add("Prius");

        // Assert
        dto.ModelosDisponibles.Should().BeEquivalentTo(new[] { "Corolla", "Yaris" });
    }
}
