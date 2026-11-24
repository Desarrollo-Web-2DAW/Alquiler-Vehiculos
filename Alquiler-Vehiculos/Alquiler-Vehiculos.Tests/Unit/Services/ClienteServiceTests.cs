using Alquiler_Vehiculos.Dto.Clientes;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Usuarios.Clientes;
using Alquiler_Vehiculos.Repositories.Clientes;
using Alquiler_Vehiculos.Services.Usuarios.Clientes;
using Alquiler_Vehiculos.Validators.Clientes;
using FluentAssertions;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Services;

/// <summary>
/// Tests unitarios del servicio de clientes con el repositorio simulado.
/// Requisitos cubiertos: RF-13, RF-14.
/// </summary>
[Category("Unit")]
public class ClienteServiceTests {
    protected Mock<IClienteRepository> RepositorioMock = null!;
    protected ClienteService Servicio = null!;

    [SetUp]
    public void SetUp() {
        RepositorioMock = new Mock<IClienteRepository>();
        Servicio = new ClienteService(
            RepositorioMock.Object,
            new ClienteCreateValidator(),
            new ClienteUpdateValidator());
    }

    protected static Cliente NuevoCliente(
        string nombre = "Ana",
        string apellidos = "García",
        string email = "ana@correo.test",
        string? dni = "12345678Z",
        bool activo = true) => new() {
        Nombre = nombre,
        Apellidos = apellidos,
        Email = email,
        Telefono = "611222333",
        FechaNacimiento = new DateOnly(1990, 1, 1),
        Dni = dni,
        Activo = activo
    };

    protected static ClienteCreateDto AltaValida() => new() {
        Nombre = "Lucía",
        Apellidos = "Navarro Prieto",
        Email = "Lucia.Navarro@Correo.TEST",
        Telefono = "611222333",
        FechaNacimiento = new DateOnly(1995, 3, 21),
        Dni = "12345678z"
    };

    protected static ClienteUpdateDto ModificacionValida() => new() {
        Nombre = "Lucía Marina",
        Apellidos = "Navarro Prieto",
        Telefono = "699111222",
        FechaNacimiento = new DateOnly(1995, 3, 21),
        Dni = "87654321x"
    };

    [TestFixture]
    public class GetPaged : ClienteServiceTests {
        [Test]
        public async Task GetPagedAsync_VariosClientes_DevuelveLaPaginaPedida() {
            // Arrange
            var clientes = new List<Cliente> {
                NuevoCliente("A", "Uno", "a@correo.test", "11111111H"),
                NuevoCliente("B", "Dos", "b@correo.test", "22222222J"),
                NuevoCliente("C", "Tres", "c@correo.test", "33333333P"),
                NuevoCliente("D", "Cuatro", "d@correo.test", "44444444A"),
                NuevoCliente("E", "Cinco", "e@correo.test", "55555555K")
            };
            RepositorioMock
                .Setup(r => r.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(clientes);

            // Act
            var resultado = await Servicio.GetPagedAsync(new ClienteFiltroDto(Page: 2, PageSize: 2));

            // Assert
            resultado.Items.Should().HaveCount(2);
            resultado.TotalItems.Should().Be(5);
            resultado.TotalPages.Should().Be(3);
        }

        [Test]
        public async Task GetPagedAsync_TextoEnElNombre_FiltraSinDistinguirMayusculas() {
            // Arrange
            var clientes = new List<Cliente> {
                NuevoCliente("Lucía", "Navarro", "lucia@correo.test", "11111111H"),
                NuevoCliente("Pedro", "Zamora", "pedro@correo.test", "22222222J")
            };
            RepositorioMock
                .Setup(r => r.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(clientes);

            // Act
            var resultado = await Servicio.GetPagedAsync(new ClienteFiltroDto(Texto: "LUC"));

            // Assert
            resultado.Items.Should().HaveCount(1);
            resultado.TotalItems.Should().Be(1);
        }

        [Test]
        public async Task GetPagedAsync_TextoEnElDocumento_FiltraSinDistinguirMayusculas() {
            // Arrange
            var clientes = new List<Cliente> {
                NuevoCliente("Lucía", "Navarro", "lucia@correo.test", "87654321X"),
                NuevoCliente("Pedro", "Zamora", "pedro@correo.test", "22222222J")
            };
            RepositorioMock
                .Setup(r => r.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(clientes);

            // Act
            var resultado = await Servicio.GetPagedAsync(new ClienteFiltroDto(Texto: "87654321x"));

            // Assert
            resultado.Items.Should().HaveCount(1);
        }

        [Test]
        public async Task GetPagedAsync_FiltroActivoFalso_DevuelveSoloLosInactivos() {
            // Arrange
            var clientes = new List<Cliente> {
                NuevoCliente("Lucía", "Navarro", "lucia@correo.test", "11111111H", activo: true),
                NuevoCliente("Pedro", "Zamora", "pedro@correo.test", "22222222J", activo: false)
            };
            RepositorioMock
                .Setup(r => r.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(clientes);

            // Act
            var resultado = await Servicio.GetPagedAsync(new ClienteFiltroDto(Activo: false));

            // Assert
            resultado.Items.Should().HaveCount(1);
            resultado.TotalItems.Should().Be(1);
        }

        [Test]
        public async Task GetPagedAsync_PaginaYTamanoFueraDeRango_LosNormaliza() {
            // Arrange
            var clientes = new List<Cliente> {
                NuevoCliente("A", "Uno", "a@correo.test", "11111111H"),
                NuevoCliente("B", "Dos", "b@correo.test", "22222222J"),
                NuevoCliente("C", "Tres", "c@correo.test", "33333333P")
            };
            RepositorioMock
                .Setup(r => r.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(clientes);

            // Act
            var resultado = await Servicio.GetPagedAsync(new ClienteFiltroDto(Page: 0, PageSize: 1000));

            // Assert
            resultado.Items.Should().HaveCount(3);
            resultado.TotalPages.Should().Be(1);
        }

        [Test]
        public async Task GetPagedAsync_SinIncluirBajas_NoLasPideAlRepositorio() {
            // Arrange
            RepositorioMock
                .Setup(r => r.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Cliente>());

            // Act
            await Servicio.GetPagedAsync(new ClienteFiltroDto());

            // Assert
            RepositorioMock.Verify(r => r.GetAllAsync(false, It.IsAny<CancellationToken>()), Times.Once);
            RepositorioMock.Verify(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task GetPagedAsync_IncluyendoBajas_LasPideAlRepositorio() {
            // Arrange
            RepositorioMock
                .Setup(r => r.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Cliente>());

            // Act
            await Servicio.GetPagedAsync(new ClienteFiltroDto(IncluirBajas: true));

            // Assert
            RepositorioMock.Verify(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [TestFixture]
    public class GetById : ClienteServiceTests {
        [Test]
        public async Task GetByIdAsync_ClienteExiste_DevuelveElCliente() {
            // Arrange
            var cliente = NuevoCliente();
            RepositorioMock
                .Setup(r => r.GetByIdAsync(cliente.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cliente);

            // Act
            var resultado = await Servicio.GetByIdAsync(cliente.Id);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Id.Should().Be(cliente.Id);
            resultado.Value.Email.Should().Be("ana@correo.test");
        }

        [Test]
        public async Task GetByIdAsync_ClienteNoExiste_DevuelveNotFound() {
            // Arrange
            var id = Guid.NewGuid();
            RepositorioMock
                .Setup(r => r.GetByIdAsync(id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Cliente?)null);

            // Act
            var resultado = await Servicio.GetByIdAsync(id);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<NotFoundError>();
            resultado.Error.Code.Should().Be("CLIENTE_NO_ENCONTRADO");
        }

        [Test]
        public async Task GetByIdAsync_IncluyendoBajas_SeLoPasaAlRepositorio() {
            // Arrange
            var cliente = NuevoCliente();
            RepositorioMock
                .Setup(r => r.GetByIdAsync(cliente.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cliente);

            // Act
            await Servicio.GetByIdAsync(cliente.Id, incluirBajas: true);

            // Assert
            RepositorioMock.Verify(
                r => r.GetByIdAsync(cliente.Id, true, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [TestFixture]
    public class Create : ClienteServiceTests {
        [Test]
        public async Task CreateAsync_DatosCorrectos_GuardaElClienteNormalizado() {
            // Arrange
            var guardado = NuevoCliente("Lucía", "Navarro Prieto", "lucia.navarro@correo.test", "12345678Z");
            RepositorioMock
                .Setup(r => r.EmailEnUsoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            RepositorioMock
                .Setup(r => r.DocumentoEnUsoAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            RepositorioMock
                .Setup(r => r.AddAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(guardado);

            // Act
            var resultado = await Servicio.CreateAsync(AltaValida());

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Email.Should().Be("lucia.navarro@correo.test");
            RepositorioMock.Verify(
                r => r.EmailEnUsoAsync("lucia.navarro@correo.test", It.IsAny<CancellationToken>()), Times.Once);
            RepositorioMock.Verify(
                r => r.AddAsync(
                    It.Is<Cliente>(c => c.Email == "lucia.navarro@correo.test" && c.Dni == "12345678Z"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task CreateAsync_DatosNoValidos_DevuelveValidationErrorYNoToca() {
            // Arrange
            var dto = AltaValida() with { Nombre = "" };

            // Act
            var resultado = await Servicio.CreateAsync(dto);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<ValidationError>();
            RepositorioMock.Verify(
                r => r.EmailEnUsoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            RepositorioMock.Verify(
                r => r.AddAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task CreateAsync_CorreoYaExiste_DevuelveConflictYNoGuarda() {
            // Arrange
            RepositorioMock
                .Setup(r => r.EmailEnUsoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var resultado = await Servicio.CreateAsync(AltaValida());

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<ConflictError>();
            resultado.Error.Code.Should().Be("EMAIL_DUPLICADO");
            RepositorioMock.Verify(
                r => r.DocumentoEnUsoAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
            RepositorioMock.Verify(
                r => r.AddAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task CreateAsync_DocumentoYaExiste_DevuelveConflictYNoGuarda() {
            // Arrange
            RepositorioMock
                .Setup(r => r.EmailEnUsoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            RepositorioMock
                .Setup(r => r.DocumentoEnUsoAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var resultado = await Servicio.CreateAsync(AltaValida());

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<ConflictError>();
            resultado.Error.Code.Should().Be("DOCUMENTO_DUPLICADO");
            RepositorioMock.Verify(
                r => r.AddAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task CreateAsync_DocumentoEnMinusculas_LoCompruebaEnMayusculas() {
            // Arrange
            RepositorioMock
                .Setup(r => r.EmailEnUsoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            RepositorioMock
                .Setup(r => r.DocumentoEnUsoAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await Servicio.CreateAsync(AltaValida());

            // Assert
            RepositorioMock.Verify(
                r => r.DocumentoEnUsoAsync("12345678Z", null, null, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [TestFixture]
    public class Update : ClienteServiceTests {
        [Test]
        public async Task UpdateAsync_DatosCorrectos_ActualizaConLosDatosNormalizados() {
            // Arrange
            var existente = NuevoCliente();
            var actualizado = NuevoCliente("Lucía Marina", "Navarro Prieto", "ana@correo.test", "87654321X");
            RepositorioMock
                .Setup(r => r.GetByIdAsync(existente.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existente);
            RepositorioMock
                .Setup(r => r.DocumentoEnUsoPorOtroAsync(
                    existente.Id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            RepositorioMock
                .Setup(r => r.UpdateAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actualizado);

            // Act
            var resultado = await Servicio.UpdateAsync(existente.Id, ModificacionValida());

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Nombre.Should().Be("Lucía Marina");
            RepositorioMock.Verify(
                r => r.UpdateAsync(
                    It.Is<Cliente>(c => c.Id == existente.Id && c.Nombre == "Lucía Marina" && c.Dni == "87654321X"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task UpdateAsync_DatosNoValidos_DevuelveValidationErrorYNoToca() {
            // Arrange
            var dto = ModificacionValida() with { Telefono = "" };

            // Act
            var resultado = await Servicio.UpdateAsync(Guid.NewGuid(), dto);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<ValidationError>();
            RepositorioMock.Verify(
                r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Never);
            RepositorioMock.Verify(
                r => r.UpdateAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task UpdateAsync_ClienteNoExiste_DevuelveNotFoundYNoActualiza() {
            // Arrange
            var id = Guid.NewGuid();
            RepositorioMock
                .Setup(r => r.GetByIdAsync(id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Cliente?)null);

            // Act
            var resultado = await Servicio.UpdateAsync(id, ModificacionValida());

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<NotFoundError>();
            RepositorioMock.Verify(
                r => r.DocumentoEnUsoPorOtroAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
            RepositorioMock.Verify(
                r => r.UpdateAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task UpdateAsync_DocumentoDeOtroCliente_DevuelveConflictYNoActualiza() {
            // Arrange
            var existente = NuevoCliente();
            RepositorioMock
                .Setup(r => r.GetByIdAsync(existente.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existente);
            RepositorioMock
                .Setup(r => r.DocumentoEnUsoPorOtroAsync(
                    existente.Id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var resultado = await Servicio.UpdateAsync(existente.Id, ModificacionValida());

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<ConflictError>();
            resultado.Error.Code.Should().Be("DOCUMENTO_DUPLICADO");
            RepositorioMock.Verify(
                r => r.UpdateAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task UpdateAsync_ElRepositorioNoLoEncuentraAlActualizar_DevuelveNotFound() {
            // Arrange
            var existente = NuevoCliente();
            RepositorioMock
                .Setup(r => r.GetByIdAsync(existente.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existente);
            RepositorioMock
                .Setup(r => r.DocumentoEnUsoPorOtroAsync(
                    existente.Id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            RepositorioMock
                .Setup(r => r.UpdateAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Cliente?)null);

            // Act
            var resultado = await Servicio.UpdateAsync(existente.Id, ModificacionValida());

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<NotFoundError>();
        }
    }

    [TestFixture]
    public class Delete : ClienteServiceTests {
        [Test]
        public async Task DeleteAsync_ClienteExiste_LoDaDeBaja() {
            // Arrange
            var id = Guid.NewGuid();
            RepositorioMock
                .Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var resultado = await Servicio.DeleteAsync(id);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            RepositorioMock.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task DeleteAsync_ClienteNoExiste_DevuelveNotFound() {
            // Arrange
            var id = Guid.NewGuid();
            RepositorioMock
                .Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var resultado = await Servicio.DeleteAsync(id);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<NotFoundError>();
        }
    }
}
