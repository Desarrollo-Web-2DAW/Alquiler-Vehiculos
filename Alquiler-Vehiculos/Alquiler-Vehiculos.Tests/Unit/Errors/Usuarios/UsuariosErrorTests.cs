using Alquiler_Vehiculos.Errors.Auth;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Usuarios;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Errors.Usuarios;

[TestFixture]
[Category("Unit")]
public class UsuariosErrorTests {
    private static readonly Guid Id = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string Email = "persona@example.com";

    public static IEnumerable<TestCaseData> Errores() {
        yield return Caso(() => ClienteError.NotFound(Id), new NotFoundError("CLIENTE_NO_ENCONTRADO", $"Cliente con ID '{Id}' no encontrado"), "ClientePorId");
        yield return Caso(() => ClienteError.NotFoundByEmail(Email), new NotFoundError("CLIENTE_NO_ENCONTRADO", $"Cliente con email '{Email}' no encontrado"), "ClientePorEmail");
        yield return Caso(() => ClienteError.EmailExistente(Email), new ConflictError("EMAIL_DUPLICADO", $"Ya existe un cliente con el email '{Email}'"), "ClienteEmailDuplicado");
        yield return Caso(() => ClienteError.TelefonoExistente("600123456"), new ConflictError("TELEFONO_DUPLICADO", "Ya existe un cliente con el teléfono '600123456'"), "ClienteTelefonoDuplicado");
        yield return Caso(() => ClienteError.DocumentoExistente("12345678Z"), new ConflictError("DOCUMENTO_DUPLICADO", "Ya existe un cliente con el documento '12345678Z'"), "ClienteDocumentoDuplicado");
        yield return Caso(() => ClienteError.ConReservasVivas(Id), new ConflictError("CLIENTE_CON_RESERVAS_VIVAS", $"El cliente con ID '{Id}' tiene reservas vivas y no puede darse de baja"), "ClienteConReservas");
        yield return Caso(() => ClienteError.ClienteInactivo(Id), new BusinessRuleError("CLIENTE_INACTIVO", $"El cliente con ID '{Id}' está inactivo y no puede realizar reservas"), "ClienteInactivo");
        yield return Caso(() => ClienteError.ClienteMenorDeEdad(17), new BusinessRuleError("CLIENTE_MENOR_DE_EDAD", "El cliente tiene 17 años. Debe ser mayor de edad para alquilar."), "ClienteMenor");
        yield return Caso(() => ClienteError.FechaNacimientoInvalida(), ValidationError.Create("La fecha de nacimiento no puede ser una fecha futura."), "FechaNacimiento");
        yield return Caso(() => TrabajadorError.NotFound(Id), new NotFoundError("TRABAJADOR_NO_ENCONTRADO", $"Trabajador con ID '{Id}' no encontrado"), "TrabajadorPorId");
        yield return Caso(() => TrabajadorError.NotFoundByEmail(Email), new NotFoundError("TRABAJADOR_NO_ENCONTRADO", $"Trabajador con email '{Email}' no encontrado"), "TrabajadorPorEmail");
        yield return Caso(() => TrabajadorError.EmailExistente(Email), new ConflictError("EMAIL_DUPLICADO", $"Ya existe un trabajador con el email '{Email}'"), "TrabajadorEmailDuplicado");
        yield return Caso(() => TrabajadorError.DniExistente("12345678Z"), new ConflictError("DOCUMENTO_DUPLICADO", "Ya existe un trabajador con el DNI '12345678Z'"), "TrabajadorDniDuplicado");
        yield return Caso(() => TrabajadorError.EsResponsableOficina(Id), new ConflictError("TRABAJADOR_ES_RESPONSABLE", $"El trabajador con ID '{Id}' es el responsable actual de la oficina y no puede darse de baja hasta nombrar a otro"), "ResponsableOficina");
        yield return Caso(() => TrabajadorError.ConReservasVivas(Id), new ConflictError("TRABAJADOR_CON_RESERVAS_VIVAS", $"El trabajador con ID '{Id}' tiene reservas vivas asociadas y no puede darse de baja"), "TrabajadorConReservas");
        yield return Caso(() => TrabajadorError.TrabajadorInactivo(Id), new BusinessRuleError("TRABAJADOR_INACTIVO", $"El trabajador con ID '{Id}' está inactivo y no puede operar"), "TrabajadorInactivo");
        yield return Caso(() => UsuarioError.NotFound(Id), new NotFoundError("USUARIO_NO_ENCONTRADO", $"Usuario con ID '{Id}' no encontrado"), "UsuarioPorId");
        yield return Caso(() => UsuarioError.UltimoAdmin(), new ConflictError("ULTIMO_ADMIN", "No se puede dejar el sistema sin ningún administrador activo"), "UltimoAdmin");
        yield return Caso(() => UsuarioError.RolInvalido("Invitado"), ValidationError.Create("El rol 'Invitado' no es válido. Valores permitidos: Cliente, Trabajador, Responsable, Admin"), "RolInvalido");
        yield return Caso(() => UsuarioError.RolIncompatibleConPersona("Admin", Id), new ConflictError("ROL_INCOMPATIBLE_CON_PERSONA", $"El rol 'Admin' no es compatible con la persona de la cuenta '{Id}'. Un cliente solo puede ser Cliente; un trabajador puede ser Trabajador, Responsable o Admin."), "RolIncompatible");
        yield return Caso(() => AuthError.CredencialesInvalidas(), new UnauthorizedError("CREDENCIALES_INVALIDAS", "Email o contraseña incorrectos"), "CredencialesInvalidas");
        yield return Caso(() => AuthError.CuentaInactiva(), new ForbiddenError("CUENTA_INACTIVA", "La cuenta está suspendida o dada de baja"), "CuentaInactiva");
        yield return Caso(() => AuthError.CuentaBloqueada(), new ForbiddenError("CUENTA_BLOQUEADA", "Demasiados intentos fallidos. Inténtalo más tarde"), "CuentaBloqueada");
        yield return Caso(() => AuthError.CuentaSinRol(), new ForbiddenError("CUENTA_SIN_ROL", "La cuenta no tiene ningún rol asignado"), "CuentaSinRol");
        yield return Caso(() => AuthError.EmailExistente(Email), new ConflictError("EMAIL_DUPLICADO", $"Ya existe una cuenta con el email '{Email}'"), "AuthEmailDuplicado");
        yield return Caso(() => AuthError.DocumentoExistente("12345678Z"), new ConflictError("DOCUMENTO_DUPLICADO", "Ya existe una persona con el documento '12345678Z'"), "AuthDocumentoDuplicado");
        yield return Caso(() => AuthError.Validacion("Dato incorrecto"), ValidationError.Create("Dato incorrecto"), "AuthValidacion");
    }

    private static TestCaseData Caso(Func<DomainError> crear, DomainError esperado, string nombre) =>
        new TestCaseData(crear, esperado).SetName($"Factoria_{nombre}_ConservaTipoCodigoYMensaje");

    [TestCaseSource(nameof(Errores))]
    public void Factoria_DatosDelError_ConservaTipoCodigoYMensaje(Func<DomainError> crear, DomainError esperado) {
        // Arrange
        var tipo = esperado.GetType();

        // Act
        var resultado = crear();

        // Assert
        resultado.Should().BeOfType(tipo);
        resultado.Should().BeEquivalentTo(esperado);
    }

    [TestCase("auth")]
    [TestCase("usuario")]
    public void ValidacionConCampos_ConErrores_ConservaDetalle(string origen) {
        // Arrange
        var errores = new Dictionary<string, string[]> { ["Email"] = ["Formato incorrecto", "Email obligatorio"] };

        // Act
        var resultado = origen == "auth" ? AuthError.ValidacionConCampos(errores) : UsuarioError.ValidacionConCampos(errores);

        // Assert
        resultado.Code.Should().Be("VALIDACION");
        resultado.Message.Should().Be("Errores de validación");
        resultado.ValidationErrors.Should().BeSameAs(errores);
    }
}
