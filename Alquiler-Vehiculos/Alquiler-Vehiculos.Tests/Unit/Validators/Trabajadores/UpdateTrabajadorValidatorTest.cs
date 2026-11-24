using Alquiler_Vehiculos.Dto.Trabajadores;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Validators.Trabajadores;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validator;


[TestFixture]
[Category("Unit")]
public class UpdateTrabajadorValidatorTests {
    private UpdateTrabajadorValidator _validator = null!;

    [SetUp]
    public void SetUp() {
        _validator = new UpdateTrabajadorValidator();
    }

    private static UpdateTrabajadorDto CrearDtoValido() {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        return new UpdateTrabajadorDto {
            Nombre = "Carlos",
            Apellidos = "Ruiz Navarro",
            Email = "carlos.ruiz@empresa.es",
            Telefono = "622333444",
            FechaNacimiento = hoy.AddYears(-30),
            Dni = "11111111H",
            FechaIngreso = hoy.AddYears(-2),
            Tipo = TipoTrabajador.Mecanico
        };
    }

    // ── Casos positivos ──────────────────────────────────────────────────

    [Test]
    public void Validate_DtoValido_EsValido() {
        // Arrange
        var dto = CrearDtoValido();

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [TestCase(TipoTrabajador.Operaciones)]
    [TestCase(TipoTrabajador.Mecanico)]
    public void Validate_TiposPermitidos_EsValido(TipoTrabajador tipo) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Tipo = tipo;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_TelefonoConPrefijoYDniEnMinuscula_EsValido() {
        // Arrange
        var dto = CrearDtoValido();
        dto.Telefono = "+34 622 333 444";
        dto.Dni = "11111111h";

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    // ── Casos negativos ──────────────────────────────────────────────────

    [TestCase("")]
    [TestCase("C")]
    public void Validate_NombreInvalido_FallaEnNombre(string nombreInvalido) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Nombre = nombreInvalido;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTrabajadorDto.Nombre));
    }

    [TestCase("")]
    [TestCase("R")]
    public void Validate_ApellidosInvalidos_FallaEnApellidos(string apellidosInvalidos) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Apellidos = apellidosInvalidos;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTrabajadorDto.Apellidos));
    }

    [TestCase("")]
    [TestCase("email-no-valido")]
    public void Validate_EmailInvalido_FallaEnEmail(string emailInvalido) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Email = emailInvalido;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTrabajadorDto.Email));
    }

    [TestCase("")]
    [TestCase("123")]
    [TestCase("622ABC444")]
    public void Validate_TelefonoInvalido_FallaEnTelefono(string telefonoInvalido) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Telefono = telefonoInvalido;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTrabajadorDto.Telefono));
    }

    [Test]
    public void Validate_MenorDeEdad_FallaEnFechaNacimiento() {
        // Arrange
        var dto = CrearDtoValido();
        dto.FechaNacimiento = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-17);

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTrabajadorDto.FechaNacimiento));
    }

    [Test]
    public void Validate_FechaNacimientoAnteriorA100Anios_FallaEnFechaNacimiento() {
        // Arrange
        var dto = CrearDtoValido();
        dto.FechaNacimiento = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-100);

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTrabajadorDto.FechaNacimiento));
    }

    [TestCase("")]
    [TestCase("1111111H")]   // Formato incorrecto
    [TestCase("12345678A")]  // Letra de control incorrecta (RF-16)
    public void Validate_DniInvalido_FallaEnDni(string dniInvalido) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Dni = dniInvalido;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTrabajadorDto.Dni));
    }

    [Test]
    public void Validate_FechaIngresoFutura_FallaEnFechaIngreso() {
        // Arrange
        var dto = CrearDtoValido();
        dto.FechaIngreso = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTrabajadorDto.FechaIngreso));
    }

    [Test]
    public void Validate_FechaIngresoAntesDeCumplir18_FallaEnFechaIngreso() {
        // Arrange
        var dto = CrearDtoValido();
        dto.FechaIngreso = dto.FechaNacimiento.AddYears(18).AddDays(-1);

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTrabajadorDto.FechaIngreso));
    }

    [Test]
    public void Validate_TipoFueraDeEnum_FallaEnTipo() {
        // Arrange
        var dto = CrearDtoValido();
        dto.Tipo = (TipoTrabajador)99;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTrabajadorDto.Tipo));
    }
}