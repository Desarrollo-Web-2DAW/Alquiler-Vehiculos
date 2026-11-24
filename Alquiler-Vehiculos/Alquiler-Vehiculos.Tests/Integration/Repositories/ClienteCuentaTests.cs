using Alquiler_Vehiculos.Models.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios.Clientes;
using Alquiler_Vehiculos.Repositories.Clientes;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Integration.Repositories;

/// <summary>
/// Pruebas de integración de la consulta del cliente al que pertenece una cuenta,
/// contra PostgreSQL real. Sirve para que un cliente solo acceda a sus datos.
/// </summary>
[Category("Integration")]
public class ClienteCuentaTests : IntegrationTestBase {
    protected ClienteRepository Repositorio = null!;

    [SetUp]
    public void SetUpRepositorio() {
        Repositorio = new ClienteRepository(Db);
    }

    [TestFixture]
    public class GetClienteIdDeUsuario : ClienteCuentaTests {
        [Test]
        public async Task GetClienteIdDeUsuarioAsync_CuentaDeUnCliente_DevuelveSuIdentificador() {
            // Arrange
            var cliente = new Cliente {
                Nombre = "Ana",
                Apellidos = "García",
                Email = "ana@correo.test",
                Telefono = "611222333",
                FechaNacimiento = new DateOnly(1990, 1, 1),
                Dni = "12345678Z"
            };
            Db.Clientes.Add(cliente);
            var cuenta = new User { UserName = "ana@correo.test", Email = "ana@correo.test", ClienteId = cliente.Id };
            Db.Users.Add(cuenta);
            await Db.SaveChangesAsync();

            // Act
            var clienteId = await Repositorio.GetClienteIdDeUsuarioAsync(cuenta.Id);

            // Assert
            clienteId.Should().Be(cliente.Id);
        }

        [Test]
        public async Task GetClienteIdDeUsuarioAsync_CuentaSinCliente_DevuelveNull() {
            // Arrange
            var cuenta = new User { UserName = "admin@correo.test", Email = "admin@correo.test" };
            Db.Users.Add(cuenta);
            await Db.SaveChangesAsync();

            // Act
            var clienteId = await Repositorio.GetClienteIdDeUsuarioAsync(cuenta.Id);

            // Assert
            clienteId.Should().BeNull();
        }

        [Test]
        public async Task GetClienteIdDeUsuarioAsync_CuentaInexistente_DevuelveNull() {
            // Arrange

            // Act
            var clienteId = await Repositorio.GetClienteIdDeUsuarioAsync(Guid.NewGuid());

            // Assert
            clienteId.Should().BeNull();
        }
    }
}