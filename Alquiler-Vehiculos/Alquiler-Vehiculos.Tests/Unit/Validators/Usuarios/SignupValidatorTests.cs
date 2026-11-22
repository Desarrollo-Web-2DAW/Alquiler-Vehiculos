using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Validators.Usuarios;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators.Usuarios;

[TestFixture]
[Category("Unit")]
public class SignupValidatorTests {
    private static SignupDto CrearDto() => new() {
        Email = "cliente@example.com", Password = "Ejemplo123!", Nombre = "Lucia",
        Apellidos = "Fuertes", Telefono = "+34 600123456", FechaNacimiento = new(2000, 1, 1),
        Dni = "12345678Z"
    };

    [TestCase("dni")]
    [TestCase("nie")]
    [TestCase("pasaporte")]
    public void Validate_UnDocumentoIndependiente_NoDevuelveErrores(string documento) {
        // Arrange
        var validator = new SignupValidator();
        var dto = CrearDto() with {
            Dni = documento == "dni" ? "12345678Z" : null,
            Nie = documento == "nie" ? "X1234567L" : null,
            Pasaporte = documento == "pasaporte" ? "ABC123" : null
        };

        // Act
        var resultado = validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [TestCase("", "La contraseña es obligatoria")]
    [TestCase("Aa1!", "La contraseña debe tener al menos 8 caracteres")]
    [TestCase("ejemplo123!", "La contraseña debe tener al menos una mayúscula")]
    [TestCase("EJEMPLO123!", "La contraseña debe tener al menos una minúscula")]
    [TestCase("Ejemploabc!", "La contraseña debe tener al menos un número")]
    [TestCase("Ejemplo1234", "La contraseña debe tener al menos un símbolo")]
    public void Validate_PasswordInvalida_DevuelveReglaIncumplida(string password, string mensaje) {
        // Arrange
        var validator = new SignupValidator();
        var dto = CrearDto() with { Password = password };

        // Act
        var resultado = validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage == mensaje);
    }

    [TestCase("Email", "")]
    [TestCase("Email", "incorrecto")]
    [TestCase("Email", "largo")]
    [TestCase("Nombre", "")]
    [TestCase("Nombre", "largo")]
    [TestCase("Apellidos", "")]
    [TestCase("Apellidos", "largo")]
    [TestCase("Telefono", "")]
    [TestCase("Telefono", "123")]
    [TestCase("Telefono", "abcdefghi")]
    [TestCase("Telefono", "largo")]
    public void Validate_CampoInvalido_DevuelveErrorDelCampo(string campo, string valor) {
        // Arrange
        var validator = new SignupValidator();
        var dto = CrearDto();
        dto = campo switch {
            "Email" => dto with { Email = valor == "largo" ? new string('a', 150) + "@a.es" : valor },
            "Nombre" => dto with { Nombre = valor == "largo" ? new string('a', 101) : valor },
            "Apellidos" => dto with { Apellidos = valor == "largo" ? new string('a', 151) : valor },
            _ => dto with { Telefono = valor == "largo" ? new string('1', 21) : valor }
        };

        // Act
        var resultado = validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == campo);
    }

    [TestCase("vacia")]
    [TestCase("hoy")]
    [TestCase("futura")]
    public void Validate_FechaNoAdmitida_DevuelveErrorDeNacimiento(string caso) {
        // Arrange
        var validator = new SignupValidator();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var dto = CrearDto() with { FechaNacimiento = caso == "vacia" ? default : hoy.AddDays(caso == "futura" ? 1 : 0) };

        // Act
        var resultado = validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "FechaNacimiento");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_SinDocumentos_DevuelveErrorDeDocumentos(string? documento) {
        // Arrange
        var validator = new SignupValidator();
        var dto = CrearDto() with { Dni = documento, Nie = documento, Pasaporte = documento };

        // Act
        var resultado = validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorMessage == "Debe indicar al menos un documento: DNI, NIE o pasaporte");
    }
}
