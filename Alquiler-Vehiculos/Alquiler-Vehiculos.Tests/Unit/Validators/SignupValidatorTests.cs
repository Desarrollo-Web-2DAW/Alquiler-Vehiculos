using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Validators.Usuarios;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators;

/// <summary>
/// Tests unitarios del validador de signup (RF-13, RF-14).
/// Comprueba las reglas de formato del alta de cliente.
/// </summary>
[Category("Unit")]
public class SignupValidatorTests {
    private SignupValidator _validator = null!;

    [SetUp]
    public void Setup() {
        _validator = new SignupValidator();
    }

    /// <summary>
    /// Devuelve un SignupDto con los campos base válidos y valores personalizados.
    /// </summary>
    private static SignupDto SignupCon(
        string email = "cliente@ejemplo.com",
        string password = "Contraseña1!",
        string nombre = "Ana",
        string apellidos = "García López",
        string telefono = "600123456",
        DateOnly? fechaNacimiento = null,
        string? dni = "12345678A",
        string? nie = null,
        string? pasaporte = null) {
        return new SignupDto {
            Email = email,
            Password = password,
            Nombre = nombre,
            Apellidos = apellidos,
            Telefono = telefono,
            FechaNacimiento = fechaNacimiento ?? new DateOnly(2000, 6, 15),
            Dni = dni,
            Nie = nie,
            Pasaporte = pasaporte
        };
    }

    #region Caso válido

    /// <summary>
    /// Dado un SignupDto con todos los campos válidos y DNI,
    /// cuando se valida, entonces la validación es correcta.
    /// </summary>
    [Test]
    public async Task ValidateAsync_DatosValidosConDni_EsValido() {
        // Arrange
        var dto = SignupCon();

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Dado un SignupDto con NIE en lugar de DNI, cuando se valida,
    /// entonces la validación es correcta (RF-14: documentos independientes).
    /// </summary>
    [Test]
    public async Task ValidateAsync_DatosValidosConNie_EsValido() {
        // Arrange
        var dto = SignupCon(dni: null, nie: "X1234567B");

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Dado un SignupDto con pasaporte en lugar de DNI/NIE, cuando se valida,
    /// entonces la validación es correcta (RF-14).
    /// </summary>
    [Test]
    public async Task ValidateAsync_DatosValidosConPasaporte_EsValido() {
        // Arrange
        var dto = SignupCon(dni: null, pasaporte: "AB1234567");

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    #endregion

    #region Email

    /// <summary>
    /// Dado un SignupDto con email vacío, cuando se valida,
    /// entonces falla con error en el campo Email.
    /// </summary>
    [Test]
    public async Task ValidateAsync_EmailVacio_FallaConErrorEmail() {
        // Arrange
        var dto = SignupCon(email: "");

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    /// <summary>
    /// Dado un SignupDto con email sin formato válido, cuando se valida,
    /// entonces falla con error de formato.
    /// </summary>
    [Test]
    public async Task ValidateAsync_EmailSinFormato_FallaConErrorFormato() {
        // Arrange
        var dto = SignupCon(email: "no-es-email");

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    #endregion

    #region Password

    /// <summary>
    /// Dado un SignupDto con contraseña vacía, cuando se valida,
    /// entonces falla con error en el campo Password.
    /// </summary>
    [Test]
    public async Task ValidateAsync_ContraseñaVacia_FallaConErrorPassword() {
        // Arrange
        var dto = SignupCon(password: "");

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    /// <summary>
    /// Dado un SignupDto con contraseña sin mayúscula, cuando se valida,
    /// entonces falla indicando que falta mayúscula.
    /// </summary>
    [Test]
    public async Task ValidateAsync_ContraseñaSinMayuscula_FallaConErrorMayuscula() {
        // Arrange
        var dto = SignupCon(password: "contraseña1!");

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("mayúscula"));
    }

    /// <summary>
    /// Dado un SignupDto con contraseña sin minúscula, cuando se valida,
    /// entonces falla indicando que falta minúscula.
    /// </summary>
    [Test]
    public async Task ValidateAsync_ContraseñaSinMinuscula_FallaConErrorMinuscula() {
        // Arrange
        var dto = SignupCon(password: "CONTRASEÑA1!");

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("minúscula"));
    }

    /// <summary>
    /// Dado un SignupDto con contraseña sin número, cuando se valida,
    /// entonces falla indicando que falta número.
    /// </summary>
    [Test]
    public async Task ValidateAsync_ContraseñaSinNumero_FallaConErrorNumero() {
        // Arrange
        var dto = SignupCon(password: "Contrastena!!");

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("número"));
    }

    /// <summary>
    /// Dado un SignupDto con contraseña sin símbolo, cuando se valida,
    /// entonces falla indicando que falta símbolo.
    /// </summary>
    [Test]
    public async Task ValidateAsync_ContraseñaSinSimbolo_FallaConErrorSimbolo() {
        // Arrange
        var dto = SignupCon(password: "Contrastena12");

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("símbolo"));
    }

    /// <summary>
    /// Dado un SignupDto con contraseña de menos de 8 caracteres, cuando se valida,
    /// entonces falla indicando la longitud mínima.
    /// </summary>
    [Test]
    public async Task ValidateAsync_ContraseñaDemasiadoCorta_FallaConErrorLongitud() {
        // Arrange
        var dto = SignupCon(password: "C1!a");

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("8"));
    }

    #endregion

    #region Nombre y Apellidos

    /// <summary>
    /// Dado un SignupDto con nombre vacío, cuando se valida,
    /// entonces falla con error en el campo Nombre.
    /// </summary>
    [Test]
    public async Task ValidateAsync_NombreVacio_FallaConErrorNombre() {
        // Arrange
        var dto = SignupCon(nombre: "");

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }

    /// <summary>
    /// Dado un SignupDto con apellidos vacíos, cuando se valida,
    /// entonces falla con error en el campo Apellidos.
    /// </summary>
    [Test]
    public async Task ValidateAsync_ApellidosVacios_FallaConErrorApellidos() {
        // Arrange
        var dto = SignupCon(apellidos: "");

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Apellidos");
    }

    #endregion

    #region Teléfono

    /// <summary>
    /// Dado un SignupDto con teléfono vacío, cuando se valida,
    /// entonces falla con error en el campo Telefono.
    /// </summary>
    [Test]
    public async Task ValidateAsync_TelefonoVacio_FallaConErrorTelefono() {
        // Arrange
        var dto = SignupCon(telefono: "");

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Telefono");
    }

    /// <summary>
    /// Dado un SignupDto con teléfono con letras, cuando se valida,
    /// entonces falla con error de formato.
    /// </summary>
    [Test]
    public async Task ValidateAsync_TelefonoConLetras_FallaConErrorFormato() {
        // Arrange
        var dto = SignupCon(telefono: "abc123456");

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Telefono");
    }

    #endregion

    #region Fecha de nacimiento

    /// <summary>
    /// Dado un SignupDto con fecha de nacimiento en el futuro, cuando se valida,
    /// entonces falla con error de fecha.
    /// </summary>
    [Test]
    public async Task ValidateAsync_FechaNacimientoEnFuturo_FallaConErrorFecha() {
        // Arrange
        var dto = SignupCon(fechaNacimiento: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "FechaNacimiento");
    }

    /// <summary>
    /// Dado un SignupDto con fecha de nacimiento hoy, cuando se valida,
    /// entonces falla porque debe ser anterior a hoy.
    /// </summary>
    [Test]
    public async Task ValidateAsync_FechaNacimientoHoy_FallaConErrorFecha() {
        // Arrange
        var dto = SignupCon(fechaNacimiento: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "FechaNacimiento");
    }

    #endregion

    #region Documentos (RF-14)

    /// <summary>
    /// Dado un SignupDto sin ningún documento (DNI, NIE ni pasaporte),
    /// cuando se valida, entonces falla indicando que falta al menos uno.
    /// </summary>
    [Test]
    public async Task ValidateAsync_SinDocumentos_FallaConErrorDocumentos() {
        // Arrange
        var dto = SignupCon(dni: null, nie: null, pasaporte: null);

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "documentos");
    }

    /// <summary>
    /// Dado un SignupDto con DNI en blanco, cuando se valida,
    /// entonces se trata como si no hubiera documento y falla.
    /// </summary>
    [Test]
    public async Task ValidateAsync_DniEnBlanco_FallaConErrorDocumentos() {
        // Arrange
        var dto = SignupCon(dni: "   ", nie: null, pasaporte: null);

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "documentos");
    }

    #endregion

    #region Múltiples errores

    /// <summary>
    /// Dado un SignupDto con todos los campos vacíos, cuando se valida,
    /// entonces falla con múltiples errores en distintos campos.
    /// </summary>
    [Test]
    public async Task ValidateAsync_TodosLosCamposVacios_FallaConMultiplesErrores() {
        // Arrange
        var dto = new SignupDto();

        // Act
        var resultado = await _validator.ValidateAsync(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().HaveCountGreaterThanOrEqualTo(5);
    }

    #endregion
}
