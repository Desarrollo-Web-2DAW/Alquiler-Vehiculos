using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Concesionario;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Concesionario;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators;

/// <summary>Pruebas de los datos de entrada para crear un concesionario.</summary>
[TestFixture]
[Category("Unit")]
public class CreateConcesionarioValidatorTests {
    private CreateConcesionarioValidator _validator = null!;

    [SetUp]
    public void SetUp() {
        _validator = new CreateConcesionarioValidator();
    }

    private static CreateConcesionarioDto CrearDtoValido() => new() {
        Nombre = "Motor Norte",
        Ciudad = "Santander",
        Telefono = "942000111",
        Email = "contacto@motornorte.es"
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

    [TestCase("Nombre", null)]
    [TestCase("Nombre", "")]
    [TestCase("Nombre", "   ")]
    [TestCase("Ciudad", null)]
    [TestCase("Ciudad", "")]
    [TestCase("Ciudad", "   ")]
    public void Validate_NombreOCiudadSinContenido_DevuelveErrorDeCampoObligatorio(
        string propiedad, string? valor) {
        // Arrange
        var dto = CrearDtoValido();
        if (propiedad == "Nombre") dto.Nombre = valor!;
        else dto.Ciudad = valor!;
        var mensaje = propiedad == "Nombre"
            ? "El nombre no puede estar vacío."
            : "La ciudad no puede estar vacía.";

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
            resultado.Errors.Should().ContainSingle()
                .Which.Should().Match<FluentValidation.Results.ValidationFailure>(
                    e => e.PropertyName == propiedad && e.ErrorMessage == mensaje);
        }
    }

    [TestCase("612345678")]
    [TestCase("712345678")]
    [TestCase("912345678")]
    public void Validate_TelefonoPermitido_NoDevuelveErrores(string telefono) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Telefono = telefono;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_TelefonoSinContenido_DevuelveErrorDeCampoObligatorio(string? telefono) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Telefono = telefono!;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Telefono"
            && e.ErrorMessage == "El teléfono no puede estar vacío.");
        resultado.Errors.Should().OnlyContain(e => e.PropertyName == "Telefono");
    }

    [TestCase("812345678")]
    [TestCase("512345678")]
    [TestCase("61234567")]
    [TestCase("6123456789")]
    [TestCase("61234567a")]
    [TestCase("+34612345678")]
    [TestCase("612 345 678")]
    [TestCase(" 612345678")]
    [TestCase("612345678 ")]
    [TestCase("612345678\n")]
    [TestCase("612345678\r\n")]
    public void Validate_TelefonoConFormatoNoPermitido_DevuelveError(string telefono) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Telefono = telefono;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle(e => e.PropertyName == "Telefono"
            && e.ErrorMessage == "El teléfono debe tener 9 cifras y empezar por 6, 7 o 9");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_EmailSinContenido_DevuelveErrorDeCampoObligatorio(string? email) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Email = email!;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Email"
            && e.ErrorMessage == "El email no puede estar vacio.");
        resultado.Errors.Should().OnlyContain(e => e.PropertyName == "Email");
    }

    [TestCase("contacto.example.com")]
    [TestCase("@example.com")]
    [TestCase("contacto@")]
    [TestCase("contacto@@example.com")]
    public void Validate_EmailConFormatoInvalido_DevuelveError(string email) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Email = email;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle(e => e.PropertyName == "Email"
            && e.ErrorMessage == "El email no tiene un formato válido");
    }

    // Documenta la comprobación básica de EmailAddress(), sin imponer una regex más estricta.
    [TestCase("contacto@example.com")]
    [TestCase("contacto@empresa")]
    [TestCase("con tacto@example.com")]
    public void Validate_EmailAceptadoPorLaComprobacionBasica_NoDevuelveErrores(string email) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Email = email;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [Test]
    public void Validate_VariosCamposInvalidos_DevuelveErroresDeTodosLosCampos() {
        // Arrange
        var dto = new CreateConcesionarioDto();

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Select(e => e.PropertyName).Distinct()
            .Should().BeEquivalentTo("Nombre", "Ciudad", "Telefono", "Email");
    }
}
