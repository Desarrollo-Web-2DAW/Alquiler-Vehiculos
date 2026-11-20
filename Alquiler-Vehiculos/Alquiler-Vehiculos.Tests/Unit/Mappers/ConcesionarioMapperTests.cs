using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Concesionario;
using Alquiler_Vehiculos.Mappers.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Mappers;

/// <summary>
/// Tests unitarios de <see cref="ConcesionarioMapper"/>.
/// Cubren la traducción DTO → modelo, la traducción modelo → DTO y las garantías
/// que añade el mapper (id opcional y campos de auditoría).
/// </summary>
public class ConcesionarioMapperTests
{
    private static CreateConcesionarioDto CrearDto() => new()
    {
        Nombre = "Motor Norte",
        Ciudad = "Santander",
        Telefono = "+34 942 000 111",
        Email = "contacto@motornorte.es"
    };

    private static ConcesionarioModel CrearModelo() =>
        new(10, "Motor Norte", "Santander", "+34 942 000 111", "contacto@motornorte.es",
            false, new DateTime(2025, 1, 1), null, null);

    [Test]
    public void ToConcesionario_ConDtoValido_CopiaLasPropiedadesDeNegocio()
    {
        // Arrange
        var dto = CrearDto();

        // Act
        var modelo = dto.ToConcesionario(10);

        // Assert
        modelo.Should().BeEquivalentTo(new
        {
            Id = 10L,
            Nombre = "Motor Norte",
            Ciudad = "Santander",
            Telefono = "+34 942 000 111",
            Email = "contacto@motornorte.es"
        });
    }

    [Test]
    public void ToConcesionario_SinId_AsignaIdCero()
    {
        // Arrange
        var dto = CrearDto();

        // Act
        var modelo = dto.ToConcesionario();

        // Assert
        modelo.Id.Should().Be(0L);
    }

    [Test]
    public void ToConcesionario_Siempre_MarcaElRegistroComoNoBorrado()
    {
        // Arrange
        var dto = CrearDto();

        // Act
        var modelo = dto.ToConcesionario();

        // Assert
        modelo.IsDeleted.Should().BeFalse();
    }

    [Test]
    public void ToConcesionario_CuandoSeCrea_RegistraLaFechaActualDeCreacion()
    {
        // Arrange
        var antes = DateTime.UtcNow;

        // Act
        var modelo = CrearDto().ToConcesionario();

        // Assert
        modelo.CreatedAt.Should().BeCloseTo(antes, TimeSpan.FromMinutes(1));
    }

    [Test]
    public void ToConcesionario_CuandoSeCrea_NoTieneFechaDeModificacionNiDeBorrado()
    {
        // Arrange
        var dto = CrearDto();

        // Act
        var modelo = dto.ToConcesionario();

        // Assert
        (modelo.UpdatedAt, modelo.DeletedAt).Should().Be((null, null));
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
            Nombre = "Motor Norte",
            Ciudad = "Santander",
            Telefono = "+34 942 000 111",
            Email = "contacto@motornorte.es"
        });
    }
}
