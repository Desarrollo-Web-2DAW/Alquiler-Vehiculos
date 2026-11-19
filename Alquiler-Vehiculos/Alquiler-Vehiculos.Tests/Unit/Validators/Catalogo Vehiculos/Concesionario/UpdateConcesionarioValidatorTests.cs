using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Concesionario;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Concesionario;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators;

/// <summary>Comprueba las reglas de actualización completa de un concesionarioModel.</summary>
[TestFixture]
[Category("Unit")]
public class UpdateConcesionarioValidatorTests {
    private UpdateConcesionarioValidator _validator = null!;

    [SetUp]
    public void SetUp() {
        _validator = new UpdateConcesionarioValidator();
    }

    private static UpdateConcesionarioDto CrearDtoValido() => new() {
        Nombre = "Motor Norte", Ciudad = "Santander",
        Telefono = "942000111", Email = "contacto@motornorte.es"
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
    [TestCase("Ciudad", null, "La ciudad no puede estar vacía.")]
    [TestCase("Ciudad", "", "La ciudad no puede estar vacía.")]
    [TestCase("Ciudad", "   ", "La ciudad no puede estar vacía.")]
    [TestCase("Telefono", null, "El teléfono no puede estar vacío.")]
    [TestCase("Telefono", "", "El teléfono no puede estar vacío.")]
    [TestCase("Telefono", "   ", "El teléfono no puede estar vacío.")]
    [TestCase("Email", null, "El email no puede estar vacio.")]
    [TestCase("Email", "", "El email no puede estar vacio.")]
    [TestCase("Email", "   ", "El email no puede estar vacio.")]
    public void Validate_CampoSinContenido_DevuelveErrorDeCampoObligatorio(
        string propiedad, string? valor, string mensaje) {
        // Arrange
        var dto = CrearDtoValido();
        switch (propiedad) {
            case "Nombre": dto.Nombre = valor!; break;
            case "Ciudad": dto.Ciudad = valor!; break;
            case "Telefono": dto.Telefono = valor!; break;
            case "Email": dto.Email = valor!; break;
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
    [TestCase("Ciudad", 1, false)]
    [TestCase("Ciudad", 2, true)]
    [TestCase("Ciudad", 100, true)]
    [TestCase("Ciudad", 101, false)]
    public void Validate_LongitudDeNombreOCiudad_RespetaLosLimites(
        string propiedad, int longitud, bool esValido) {
        // Arrange
        var dto = CrearDtoValido();
        var valor = new string('a', longitud);
        if (propiedad == "Nombre") dto.Nombre = valor;
        else dto.Ciudad = valor;
        var campo = propiedad == "Nombre" ? "El nombre" : "La ciudad";
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

    [TestCase("612345678", true)]
    [TestCase("712345678", true)]
    [TestCase("912345678", true)]
    [TestCase("812345678", false)]
    [TestCase("512345678", false)]
    [TestCase("61234567", false)]
    [TestCase("6123456789", false)]
    [TestCase("61234567a", false)]
    [TestCase("+34612345678", false)]
    [TestCase("612 345 678", false)]
    [TestCase(" 612345678", false)]
    [TestCase("612345678 ", false)]
    [TestCase("612345678\n", false)]
    [TestCase("612345678\r\n", false)]
    public void Validate_FormatoDeTelefono_RespetaElPatron(string telefono, bool esValido) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Telefono = telefono;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().Be(esValido);
        if (esValido) resultado.Errors.Should().BeEmpty();
        else {
            resultado.Errors.Should().ContainSingle(e => e.PropertyName == "Telefono"
                && e.ErrorMessage == "El teléfono debe tener 9 cifras y empezar por 6, 7 o 9");
        }
    }

    [TestCase("contacto@example.com", true)]
    [TestCase("contacto@empresa", true)]
    [TestCase("con tacto@example.com", true)]
    [TestCase("contacto.example.com", false)]
    [TestCase("@example.com", false)]
    [TestCase("contacto@", false)]
    [TestCase("contacto@@example.com", false)]
    public void Validate_FormatoDeEmail_AplicaLaComprobacionBasica(string email, bool esValido) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Email = email;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().Be(esValido);
        if (esValido) resultado.Errors.Should().BeEmpty();
        else {
            resultado.Errors.Should().ContainSingle(e => e.PropertyName == "Email"
                && e.ErrorMessage == "El email no tiene un formato válido");
        }
    }

    [Test]
    public void Validate_VariosCamposInvalidos_DevuelveErroresDeTodosLosCampos() {
        // Arrange
        var dto = new UpdateConcesionarioDto();

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Select(e => e.PropertyName).Distinct()
            .Should().BeEquivalentTo("Nombre", "Ciudad", "Telefono", "Email");
    }
}
