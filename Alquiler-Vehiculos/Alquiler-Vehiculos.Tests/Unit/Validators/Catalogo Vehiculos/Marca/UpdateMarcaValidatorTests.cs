using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Marca;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Marca;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators;

/// <summary>Comprueba los campos y la colección de modelos al actualizar una marcaModel.</summary>
[TestFixture]
[Category("Unit")]
public class UpdateMarcaValidatorTests {
    private UpdateMarcaValidator _validator = null!;

    [SetUp]
    public void SetUp() {
        _validator = new UpdateMarcaValidator();
    }

    private static UpdateMarcaDto CrearDtoValido() => new() {
        Nombre = "Toyota", Pais = "Japón", Web = "https://www.toyota.es",
        ModelosDisponibles = new List<string> { "Corolla", "Yaris" }
    };

    [Test]
    public void Validate_DatosValidos_NoDevuelveErrores() {
        // Arrange
        var dto = CrearDtoValido();

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [TestCase("Nombre", null, "El nombre no puede estar vacío.")]
    [TestCase("Nombre", "", "El nombre no puede estar vacío.")]
    [TestCase("Nombre", "   ", "El nombre no puede estar vacío.")]
    [TestCase("Pais", null, "El pais no puede estar vacío.")]
    [TestCase("Pais", "", "El pais no puede estar vacío.")]
    [TestCase("Pais", "   ", "El pais no puede estar vacío.")]
    [TestCase("Web", null, "La web no puede estar vacía.")]
    [TestCase("Web", "", "La web no puede estar vacía.")]
    [TestCase("Web", "   ", "La web no puede estar vacía.")]
    public void Validate_CampoSinContenido_DevuelveErrorDeCampoObligatorio(
        string propiedad, string? valor, string mensaje) {
        // Arrange
        var dto = CrearDtoValido();
        switch (propiedad) {
            case "Nombre": dto.Nombre = valor!; break;
            case "Pais": dto.Pais = valor!; break;
            case "Web": dto.Web = valor!; break;
        }

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == propiedad && e.ErrorMessage == mensaje);
        resultado.Errors.Should().OnlyContain(e => e.PropertyName == propiedad);
    }

    [TestCase("Nombre", 1, false)]
    [TestCase("Nombre", 2, true)]
    [TestCase("Nombre", 100, true)]
    [TestCase("Nombre", 101, false)]
    [TestCase("Pais", 1, false)]
    [TestCase("Pais", 2, true)]
    [TestCase("Pais", 100, true)]
    [TestCase("Pais", 101, false)]
    [TestCase("Web", 1, false)]
    [TestCase("Web", 2, true)]
    [TestCase("Web", 100, true)]
    [TestCase("Web", 101, false)]
    public void Validate_LongitudDeCampo_RespetaLosLimites(
        string propiedad, int longitud, bool esValido) {
        // Arrange
        var dto = CrearDtoValido();
        var valor = new string('a', longitud);
        switch (propiedad) {
            case "Nombre": dto.Nombre = valor; break;
            case "Pais": dto.Pais = valor; break;
            case "Web": dto.Web = valor; break;
        }
        var campo = propiedad switch {
            "Nombre" => "El nombre", "Pais" => "El pais", _ => "La web"
        };
        var mensaje = longitud < 2
            ? $"{campo} no puede tener menos de 2 caracteres."
            : $"{campo} no puede tener más de 100 caracteres.";

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().Be(esValido);
        if (esValido) resultado.Errors.Should().BeEmpty();
        else {
            resultado.Errors.Should().ContainSingle(e => e.PropertyName == propiedad
                && e.ErrorMessage == mensaje);
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Validate_ColeccionNulaOVacia_DevuelveError(bool esNula) {
        // Arrange
        var dto = CrearDtoValido();
        dto.ModelosDisponibles = esNula ? null! : new List<string>();

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle(e => e.PropertyName == "ModelosDisponibles"
            && e.ErrorMessage == "La marcaModel debe tener al menos un modelo disponible");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_ModeloSinContenido_DevuelveErrorDelElemento(string? modelo) {
        // Arrange
        var dto = CrearDtoValido();
        dto.ModelosDisponibles = new List<string> { "Corolla", modelo! };

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle(e => e.PropertyName == "ModelosDisponibles[1]"
            && e.ErrorMessage == "El nombre de cada modelo no puede estar vacio.");
    }

    [Test]
    public void Validate_VariosModelosInvalidos_DevuelveElIndiceDeCadaUno() {
        // Arrange
        var dto = CrearDtoValido();
        dto.ModelosDisponibles = new List<string> { "", "Yaris", "   " };

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Select(e => e.PropertyName)
            .Should().BeEquivalentTo("ModelosDisponibles[0]", "ModelosDisponibles[2]");
        resultado.Errors.Should().OnlyContain(e => e.ErrorMessage == "El nombre de cada modelo no puede estar vacio.");
    }

    [Test]
    public void Validate_UnSoloModeloValido_NoDevuelveErrores() {
        // Arrange
        var dto = CrearDtoValido();
        dto.ModelosDisponibles = new List<string> { "Corolla" };

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [Test]
    public void Validate_WebSinFormatoDeUrl_NoCompruebaElFormato() {
        // Arrange
        var dto = CrearDtoValido();
        dto.Web = "hola";

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [Test]
    public void Validate_VariosCamposInvalidos_DevuelveErroresDeTodosLosCampos() {
        // Arrange
        var dto = new UpdateMarcaDto();

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Select(e => e.PropertyName).Distinct()
            .Should().BeEquivalentTo("Nombre", "Pais", "Web", "ModelosDisponibles");
    }
}
