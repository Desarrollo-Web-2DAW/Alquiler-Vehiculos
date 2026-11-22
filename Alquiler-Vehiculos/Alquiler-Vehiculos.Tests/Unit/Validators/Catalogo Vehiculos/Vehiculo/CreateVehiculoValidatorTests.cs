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

    // Foto es el Nombre de la foto (clave de IFotoRepository), no una URL.
    private static CreateVehiculoDto CrearDtoValido() => new() {
        Foto = "coche.jpg",
        Matricula = "1234BCD", Marca = "Toyota", Modelo = "Corolla", Acriss = "ILAH",
        Color = "Azul", PrecioBaseDia = 45.50m,
        Plazas = 5, MaleteroLitros = 361, AutonomiaKm = 700, Kilometraje = 12000,
        FechaUltimaRevision = "2026-11-01", Concesionario = 10L
    };

    [Test]
    public void Validate_DtoValido_NoDevuelveErrores() {
        // Arrange
        var dto = CrearDtoValido();

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.Errors.Should().BeEmpty();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_SinFoto_DevuelveErrorDeFotoObligatoria(string? foto) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Foto = foto!;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.Errors.Should().ContainSingle()
            .Which.Should().Match<FluentValidation.Results.ValidationFailure>(e =>
                e.PropertyName == nameof(dto.Foto) && e.ErrorMessage == "La foto es obligatoria.");
    }

    // Ejemplos reales de la tabla de RF-03 (uno por valor del enum Categoria).
    [TestCase("ILAH")]
    [TestCase("CDMD")]
    [TestCase("IFAE")]
    [TestCase("FJBD")]
    [TestCase("IWMD")]
    [TestCase("PEAV")]
    [TestCase("MTAV")]
    [TestCase("IMMD")]
    [TestCase("FKMD")]
    public void Validate_AcrissDelEnunciado_NoDevuelveErrores(string acriss) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Acriss = acriss;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.Errors.Should().BeEmpty();
    }

    [TestCase("ILA", "El código ACRISS debe constar exactamente de 4 letras.")]
    [TestCase("ILAHX", "El código ACRISS debe constar exactamente de 4 letras.")]
    [TestCase("ilah", "El código ACRISS debe constar exactamente de 4 letras.")]
    [TestCase("ALAH", "El código ACRISS contiene valores no válidos en sus letras.")] // 1ª: A no es categoría
    [TestCase("IAAH", "El código ACRISS contiene valores no válidos en sus letras.")] // 2ª: A no es tipo
    [TestCase("ILVH", "El código ACRISS contiene valores no válidos en sus letras.")] // 3ª: V no es transmisión
    [TestCase("ILAK", "El código ACRISS contiene valores no válidos en sus letras.")] // 4ª: K no es combustible
    public void Validate_AcrissFueraDeCatalogo_DevuelveErrorDeAcriss(string acriss, string mensaje) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Acriss = acriss;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert — CascadeMode.Stop garantiza un único error: el de la primera regla que falla.
        resultado.Errors.Should().ContainSingle()
            .Which.Should().Match<FluentValidation.Results.ValidationFailure>(e =>
                e.PropertyName == nameof(dto.Acriss) && e.ErrorMessage == mensaje);
    }

    [TestCase('F')] // multicombustible sin aire
    [TestCase('U')] // etanol con aire
    public void Validate_CuartaLetraDelCatalogoOficial_EsAceptada(char combustible) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Acriss = $"ILA{combustible}";

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.Errors.Should().BeEmpty();
    }

    [TestCase("1234BCA")] // vocal
    [TestCase("1234BCQ")] // Q
    [TestCase("123BCDF")]
    public void Validate_MatriculaNoValida_DevuelveErrorDeMatricula(string matricula) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Matricula = matricula;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.Errors.Should().ContainSingle(e => e.PropertyName == nameof(dto.Matricula));
    }
}
