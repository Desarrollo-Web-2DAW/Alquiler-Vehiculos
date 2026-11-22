using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios.Clientes;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Models.Usuarios;

[TestFixture]
[Category("Unit")]
public class UsuariosModelTests {
    [Test]
    public void ApiSettings_ValoresInicialesYPersonalizados_ConservaConfiguracion() {
        // Arrange
        var inicial = new ApiSettings();

        // Act
        var personalizada = new ApiSettings { Nombre = "API de pruebas", Mensaje = "Testing" };

        // Assert
        inicial.Nombre.Should().Be("Alquiler-Vehiculos API");
        inicial.Mensaje.Should().Be("Modo desarrollo");
        personalizada.Nombre.Should().Be("API de pruebas");
        personalizada.Mensaje.Should().Be("Testing");
    }

    [Test]
    public void UserDto_DatosPublicos_ConservaTodosLosCampos() {
        // Arrange
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        // Act
        var dto = new UserDto(id, "cliente@example.com", "Lucia", "Fuertes", "Cliente");

        // Assert
        dto.Id.Should().Be(id);
        dto.Email.Should().Be("cliente@example.com");
        dto.Nombre.Should().Be("Lucia");
        dto.Apellidos.Should().Be("Fuertes");
        dto.Role.Should().Be("Cliente");
    }

    [Test]
    public void ApiErrorResponse_ConYSinCampos_ConservaMensajeYDetalle() {
        // Arrange
        var errores = new Dictionary<string, string[]> { ["Nombre"] = ["Obligatorio"] };

        // Act
        var vacia = new ApiErrorResponse();
        var respuesta = new ApiErrorResponse { Message = "Datos incorrectos", Errors = errores };

        // Assert
        vacia.Message.Should().BeEmpty();
        vacia.Errors.Should().BeNull();
        respuesta.Message.Should().Be("Datos incorrectos");
        respuesta.Errors.Should().BeSameAs(errores);
    }

    [TestCase(2026, 5, 19, 17, false, true)]
    [TestCase(2026, 5, 20, 18, true, true)]
    [TestCase(2026, 5, 21, 18, true, true)]
    [TestCase(2033, 5, 19, 24, true, true)]
    [TestCase(2033, 5, 20, 25, true, false)]
    [TestCase(2033, 5, 21, 25, true, false)]
    public void Cliente_FechaDeRecogida_CalculaEdadMayoriaYRecargo(int ano, int mes, int dia, int edad, bool mayor, bool joven) {
        // Arrange
        var cliente = new Cliente { FechaNacimiento = new(2008, 5, 20) };
        var fecha = new DateOnly(ano, mes, dia);

        // Act
        var cumplida = cliente.EdadCumplida(fecha);
        var puedeAlquilar = cliente.EsMayorDeEdad(fecha);
        var tieneRecargo = cliente.EsConductorJoven(fecha);

        // Assert
        cumplida.Should().Be(edad);
        puedeAlquilar.Should().Be(mayor);
        tieneRecargo.Should().Be(joven);
    }

    [Test]
    public void Cliente_BajaLogica_ConservaIdentificacionDocumentosYAuditoria() {
        // Arrange
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();

        // Act
        var cliente = new Cliente {
            Id = id, Nombre = "Lucia", Apellidos = "Fuertes", Email = "cliente@example.com", Telefono = "600123456",
            FechaNacimiento = new(2000, 1, 1), Dni = "12345678Z", Nie = "X1234567L", Pasaporte = "ABC123",
            NumeroPermisoConducir = "PERMISO123", Activo = false, IsDeleted = true,
            CreatedAt = fecha, UpdatedAt = fecha.AddDays(1), DeletedAt = fecha.AddDays(1)
        };

        // Assert
        cliente.Should().BeEquivalentTo(new { Id = id, Nombre = "Lucia", Apellidos = "Fuertes", Email = "cliente@example.com", Telefono = "600123456",
            FechaNacimiento = new DateOnly(2000, 1, 1), Dni = "12345678Z", Nie = "X1234567L", Pasaporte = "ABC123",
            NumeroPermisoConducir = "PERMISO123", Activo = false, IsDeleted = true,
            CreatedAt = fecha, UpdatedAt = fecha.AddDays(1), DeletedAt = fecha.AddDays(1) });
    }

    [Test]
    public void Trabajador_BajaLogica_ConservaDatosYAuditoria() {
        // Arrange
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();

        // Act
        var trabajador = new Trabajador {
            Id = id, Nombre = "Ana", Apellidos = "Lopez", Email = "trabajador@example.com", Telefono = "600123456",
            FechaNacimiento = new(1990, 1, 1), Dni = "12345678Z", FechaIngreso = new(2020, 1, 1), Tipo = TipoTrabajador.Operaciones,
            Activo = false, IsDeleted = true, CreatedAt = fecha, UpdatedAt = fecha.AddDays(1), DeletedAt = fecha.AddDays(1)
        };

        // Assert
        trabajador.Should().BeEquivalentTo(new { Id = id, Nombre = "Ana", Apellidos = "Lopez", Email = "trabajador@example.com", Telefono = "600123456",
            FechaNacimiento = new DateOnly(1990, 1, 1), Dni = "12345678Z", FechaIngreso = new DateOnly(2020, 1, 1), Tipo = TipoTrabajador.Operaciones,
            Activo = false, IsDeleted = true, CreatedAt = fecha, UpdatedAt = fecha.AddDays(1), DeletedAt = fecha.AddDays(1) });
    }

    [Test]
    public void User_ConPersonaYBaja_ConservaRelacionesYAuditoria() {
        // Arrange
        var cliente = new Cliente();
        var trabajador = new Trabajador();
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var cuentaCliente = new User { ClienteId = cliente.Id, Cliente = cliente, IsDeleted = true,
            DeletedAt = fecha, CreatedAt = fecha.AddDays(-1), UpdatedAt = fecha };
        var cuentaTrabajador = new User { TrabajadorId = trabajador.Id, Trabajador = trabajador };

        // Assert
        cuentaCliente.ClienteId.Should().Be(cliente.Id);
        cuentaCliente.Cliente.Should().BeSameAs(cliente);
        cuentaCliente.IsDeleted.Should().BeTrue();
        cuentaCliente.DeletedAt.Should().Be(fecha);
        cuentaCliente.CreatedAt.Should().Be(fecha.AddDays(-1));
        cuentaCliente.UpdatedAt.Should().Be(fecha);
        cuentaTrabajador.TrabajadorId.Should().Be(trabajador.Id);
        cuentaTrabajador.Trabajador.Should().BeSameAs(trabajador);
    }
}
