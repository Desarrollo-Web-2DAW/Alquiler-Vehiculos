using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Vehiculo;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators;

[TestFixture]
[Category("Unit")]
public class CreateVehiculoValidatorTests {
    private CreateVehiculoValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateVehiculoValidator();

    private static CreateVehiculoDto CrearDtoValido() => new() {
        Foto = "https://example.com/coche.jpg",
        Matricula = "1234BCD", Marca = "Toyota", Modelo = "Corolla", Acriss = "ILAH",
        Estado = "Disponible", Color = "Azul", PrecioBaseDia = 45.50m,
        Plazas = 5, MaleteroLitros = 361, AutonomiaKm = 700, Kilometraje = 12000,
        FechaUltimaRevision = "2026-11-01", Concesionario = 10L, IsActivo = true
    };

    [TestCase(null)]
    [TestCase("")]
    [TestCase("https://example.com/coche.jpg")]
    [TestCase("http://example.com/coche.png?size=100")]
    [TestCase("ftp://example.com/coche.jpg")]
    public void Validate_FotoOpcionalOUrlAbsoluta_NoDevuelveErrores(string? foto) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Foto = foto!;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [TestCase("   ")]
    [TestCase("coche.jpg")]
    [TestCase("www.example.com/coche.jpg")]
    public void Validate_FotoSinUrlAbsoluta_DevuelveErrorDeFoto(string foto) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Foto = foto;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle(e => e.PropertyName == nameof(dto.Foto)
            && e.ErrorMessage == "La imagen debe ser una URL válida.");
        resultado.Errors.Should().OnlyContain(e => e.PropertyName == nameof(dto.Foto));
    }
}
