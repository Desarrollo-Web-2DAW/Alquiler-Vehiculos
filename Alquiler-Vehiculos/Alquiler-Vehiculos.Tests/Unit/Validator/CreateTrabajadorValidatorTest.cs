using Alquiler_Vehiculos.Dto.Trabajadores;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Validators.Trabajadores;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validator;

[TestFixture]
[Category("Unit")]
public class CreateTrabajadorValidatorTests {
    private CreateTrabajadorValidator _validator = null!;

    [SetUp]
    public void SetUp() {
        _validator = new CreateTrabajadorValidator();
    }

    private static CreateTrabajadorDto CrearDtoValido() {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        return new CreateTrabajadorDto {
            Nombre = "Laura",
            Apellidos = "García Pérez",
            Email = "laura.garcia@empresa.es",
            Password = "Password1!",
            Telefono = "611222333",
            FechaNacimiento = hoy.AddYears(-25),
            Dni = "12345678Z",
            FechaIngreso = hoy.AddYears(-1),
            Tipo = TipoTrabajador.Operaciones
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

    [Test]
    public void Validate_TrabajadorCumple18HoyEIngresaHoy_EsValido() {
        // Arrange
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var dto = CrearDtoValido();
        dto.FechaNacimiento = hoy.AddYears(-18);
        dto.FechaIngreso = hoy;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
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
    public void Validate_TelefonoConPrefijoYEspaciosYDniEnMinuscula_EsValido() {
        // Arrange
        var dto = CrearDtoValido();
        dto.Telefono = "+34 611 222 333";
        dto.Dni = "12345678z";

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    // ── Casos negativos ──────────────────────────────────────────────────

    [TestCase("")]
    [TestCase("A")]
    public void Validate_NombreInvalido_FallaEnNombre(string nombreInvalido) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Nombre = nombreInvalido;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTrabajadorDto.Nombre));
    }

    [Test]
    public void Validate_NombreSupera100Caracteres_FallaEnNombre() {
        // Arrange
        var dto = CrearDtoValido();
        dto.Nombre = new string('a', 101);

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTrabajadorDto.Nombre));
    }

    [TestCase("")]
    [TestCase("G")]
    public void Validate_ApellidosInvalidos_FallaEnApellidos(string apellidosInvalidos) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Apellidos = apellidosInvalidos;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTrabajadorDto.Apellidos));
    }

    [TestCase("")]
    [TestCase("correo-sin-arroba")]
    public void Validate_EmailInvalido_FallaEnEmail(string emailInvalido) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Email = emailInvalido;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTrabajadorDto.Email));
    }

    [TestCase("")]
    [TestCase("Pass1!")]       // Menos de 8 caracteres
    [TestCase("password1!")]   // Sin mayúscula
    [TestCase("PASSWORD1!")]   // Sin minúscula
    [TestCase("Password!!")]   // Sin número
    [TestCase("Password12")]   // Sin símbolo
    public void Validate_PasswordInvalida_FallaEnPassword(string passwordInvalida) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Password = passwordInvalida;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTrabajadorDto.Password));
    }

    [TestCase("")]
    [TestCase("12345")]        // Demasiado corto
    [TestCase("61122233A")]    // Contiene letras
    public void Validate_TelefonoInvalido_FallaEnTelefono(string telefonoInvalido) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Telefono = telefonoInvalido;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTrabajadorDto.Telefono));
    }

    [Test]
    public void Validate_MenorDeEdad_FallaEnFechaNacimiento() {
        // Arrange
        var dto = CrearDtoValido();
        dto.FechaNacimiento = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-18).AddDays(1);

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTrabajadorDto.FechaNacimiento));
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
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTrabajadorDto.FechaNacimiento));
    }

    [TestCase("")]
    [TestCase("1234567Z")]   // Formato incorrecto
    [TestCase("12345678A")]  // Letra de control incorrecta (RF-16)
    public void Validate_DniInvalido_FallaEnDni(string dniInvalido) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Dni = dniInvalido;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTrabajadorDto.Dni));
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
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTrabajadorDto.FechaIngreso));
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
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTrabajadorDto.FechaIngreso));
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
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTrabajadorDto.Tipo));
    }
}