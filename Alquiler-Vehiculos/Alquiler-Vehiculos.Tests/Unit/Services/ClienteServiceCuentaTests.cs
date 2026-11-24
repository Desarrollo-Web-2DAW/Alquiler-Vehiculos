using Alquiler_Vehiculos.Repositories.Clientes;
using FluentAssertions;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Services;

/// <summary>
/// Tests unitarios de la consulta del cliente al que pertenece una cuenta.
/// </summary>
[Category("Unit")]
public class ClienteServiceCuentaTests : ClienteServiceTests {
    [TestFixture]
    public class GetClienteIdDeUsuario : ClienteServiceCuentaTests {
        [Test]
        public async Task GetClienteIdDeUsuarioAsync_CuentaDeUnCliente_DevuelveElIdentificadorDelRepositorio() {
            // Arrange
            var usuarioId = Guid.NewGuid();
            var clienteId = Guid.NewGuid();
            RepositorioMock
                .Setup(r => r.GetClienteIdDeUsuarioAsync(usuarioId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(clienteId);

            // Act
            var resultado = await Servicio.GetClienteIdDeUsuarioAsync(usuarioId);

            // Assert
            resultado.Should().Be(clienteId);
        }

        [Test]
        public async Task GetClienteIdDeUsuarioAsync_CuentaSinCliente_DevuelveNull() {
            // Arrange
            var usuarioId = Guid.NewGuid();
            RepositorioMock
                .Setup(r => r.GetClienteIdDeUsuarioAsync(usuarioId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid?)null);

            // Act
            var resultado = await Servicio.GetClienteIdDeUsuarioAsync(usuarioId);

            // Assert
            resultado.Should().BeNull();
        }
    }
}