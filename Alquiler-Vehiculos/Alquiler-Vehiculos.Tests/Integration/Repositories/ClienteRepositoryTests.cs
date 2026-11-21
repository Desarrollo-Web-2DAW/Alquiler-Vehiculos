using Alquiler_Vehiculos.Models.Usuarios.Clientes;
using Alquiler_Vehiculos.Repositories.Clientes;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Alquiler_Vehiculos.Tests.Integration.Repositories;

/// <summary>
/// Pruebas de integración del repositorio de clientes contra PostgreSQL real.
/// Requisitos cubiertos: RF-13, RF-14.
/// </summary>
[Category("Integration")]
public class ClienteRepositoryTests : IntegrationTestBase {
    protected ClienteRepository Repositorio = null!;

    [SetUp]
    public void SetUpRepositorio() {
        Repositorio = new ClienteRepository(Db);
    }

    /// <summary>Inserta un cliente directamente en la base de datos.</summary>
    protected async Task<Cliente> CrearCliente(
        string email = "ana@correo.test",
        string? dni = "12345678Z",
        string? nie = null,
        string? pasaporte = null,
        string nombre = "Ana",
        string apellidos = "García",
        bool baja = false) {
        var cliente = new Cliente {
            Nombre = nombre,
            Apellidos = apellidos,
            Email = email,
            Telefono = "611222333",
            FechaNacimiento = new DateOnly(1990, 1, 1),
            Dni = dni,
            Nie = nie,
            Pasaporte = pasaporte
        };
        if (baja) {
            cliente.IsDeleted = true;
            cliente.DeletedAt = DateTime.UtcNow;
            cliente.Activo = false;
        }

        Db.Clientes.Add(cliente);
        await Db.SaveChangesAsync();
        return cliente;
    }

    [TestFixture]
    public class GetAll : ClienteRepositoryTests {
        [Test]
        public async Task GetAllAsync_ClientesDesordenados_DevuelveOrdenadosPorApellidosYNombre() {
            // Arrange
            await CrearCliente("c@correo.test", "11111111H", nombre: "Pedro", apellidos: "Zamora");
            await CrearCliente("b@correo.test", "22222222J", nombre: "Luis", apellidos: "Alonso");
            await CrearCliente("a@correo.test", "33333333P", nombre: "Ana", apellidos: "Alonso");

            // Act
            var clientes = await Repositorio.GetAllAsync();

            // Assert
            clientes.Should().HaveCount(3);
            clientes[0].Nombre.Should().Be("Ana");
            clientes[1].Nombre.Should().Be("Luis");
            clientes[2].Nombre.Should().Be("Pedro");
        }

        [Test]
        public async Task GetAllAsync_HayBajasYNoSePiden_LasOculta() {
            // Arrange
            await CrearCliente("a@correo.test", "11111111H");
            await CrearCliente("b@correo.test", "22222222J", baja: true);

            // Act
            var clientes = await Repositorio.GetAllAsync();

            // Assert
            clientes.Should().HaveCount(1);
            clientes[0].Email.Should().Be("a@correo.test");
        }

        [Test]
        public async Task GetAllAsync_HayBajasYSePiden_LasIncluye() {
            // Arrange
            await CrearCliente("a@correo.test", "11111111H");
            await CrearCliente("b@correo.test", "22222222J", baja: true);

            // Act
            var clientes = await Repositorio.GetAllAsync(incluirBajas: true);

            // Assert
            clientes.Should().HaveCount(2);
        }

        [Test]
        public async Task GetAllAsync_SinClientes_DevuelveListaVacia() {
            // Arrange

            // Act
            var clientes = await Repositorio.GetAllAsync();

            // Assert
            clientes.Should().BeEmpty();
        }
    }

    [TestFixture]
    public class GetById : ClienteRepositoryTests {
        [Test]
        public async Task GetByIdAsync_ClienteExiste_DevuelveElCliente() {
            // Arrange
            var creado = await CrearCliente();

            // Act
            var cliente = await Repositorio.GetByIdAsync(creado.Id);

            // Assert
            cliente.Should().NotBeNull();
            cliente.Email.Should().Be("ana@correo.test");
        }

        [Test]
        public async Task GetByIdAsync_IdInexistente_DevuelveNull() {
            // Arrange

            // Act
            var cliente = await Repositorio.GetByIdAsync(Guid.NewGuid());

            // Assert
            cliente.Should().BeNull();
        }

        [Test]
        public async Task GetByIdAsync_ClienteDeBajaYNoSePiden_DevuelveNull() {
            // Arrange
            var creado = await CrearCliente(baja: true);

            // Act
            var cliente = await Repositorio.GetByIdAsync(creado.Id);

            // Assert
            cliente.Should().BeNull();
        }

        [Test]
        public async Task GetByIdAsync_ClienteDeBajaYSePiden_DevuelveElCliente() {
            // Arrange
            var creado = await CrearCliente(baja: true);

            // Act
            var cliente = await Repositorio.GetByIdAsync(creado.Id, incluirBajas: true);

            // Assert
            cliente.Should().NotBeNull();
            cliente.IsDeleted.Should().BeTrue();
        }
    }

    [TestFixture]
    public class EmailEnUso : ClienteRepositoryTests {
        [Test]
        public async Task EmailEnUsoAsync_CorreoExistente_DevuelveTrue() {
            // Arrange
            await CrearCliente("ana@correo.test");

            // Act
            var enUso = await Repositorio.EmailEnUsoAsync("ana@correo.test");

            // Assert
            enUso.Should().BeTrue();
        }

        [Test]
        public async Task EmailEnUsoAsync_CorreoLibre_DevuelveFalse() {
            // Arrange
            await CrearCliente("ana@correo.test");

            // Act
            var enUso = await Repositorio.EmailEnUsoAsync("otra@correo.test");

            // Assert
            enUso.Should().BeFalse();
        }

        [Test]
        public async Task EmailEnUsoAsync_CorreoDeClienteDeBaja_DevuelveTrue() {
            // Arrange
            await CrearCliente("ana@correo.test", baja: true);

            // Act
            var enUso = await Repositorio.EmailEnUsoAsync("ana@correo.test");

            // Assert
            enUso.Should().BeTrue();
        }
    }

    [TestFixture]
    public class DocumentoEnUsoPorOtro : ClienteRepositoryTests {
        [Test]
        public async Task DocumentoEnUsoPorOtroAsync_DniDeOtroCliente_DevuelveTrue() {
            // Arrange
            await CrearCliente("a@correo.test", "12345678Z");
            var otro = await CrearCliente("b@correo.test", "87654321X");

            // Act
            var enUso = await Repositorio.DocumentoEnUsoPorOtroAsync(otro.Id, "12345678Z", null, null);

            // Assert
            enUso.Should().BeTrue();
        }

        [Test]
        public async Task DocumentoEnUsoPorOtroAsync_NieDeOtroCliente_DevuelveTrue() {
            // Arrange
            await CrearCliente("a@correo.test", null, nie: "X1234567L");
            var otro = await CrearCliente("b@correo.test", "87654321X");

            // Act
            var enUso = await Repositorio.DocumentoEnUsoPorOtroAsync(otro.Id, null, "X1234567L", null);

            // Assert
            enUso.Should().BeTrue();
        }

        [Test]
        public async Task DocumentoEnUsoPorOtroAsync_PasaporteDeOtroCliente_DevuelveTrue() {
            // Arrange
            await CrearCliente("a@correo.test", null, pasaporte: "AAA1234565");
            var otro = await CrearCliente("b@correo.test", "87654321X");

            // Act
            var enUso = await Repositorio.DocumentoEnUsoPorOtroAsync(otro.Id, null, null, "AAA1234565");

            // Assert
            enUso.Should().BeTrue();
        }

        [Test]
        public async Task DocumentoEnUsoPorOtroAsync_DocumentoDelPropioCliente_DevuelveFalse() {
            // Arrange
            var propio = await CrearCliente("a@correo.test", "12345678Z");

            // Act
            var enUso = await Repositorio.DocumentoEnUsoPorOtroAsync(propio.Id, "12345678Z", null, null);

            // Assert
            enUso.Should().BeFalse();
        }

        [Test]
        public async Task DocumentoEnUsoPorOtroAsync_SinDocumentos_DevuelveFalse() {
            // Arrange
            var propio = await CrearCliente("a@correo.test", "12345678Z");

            // Act
            var enUso = await Repositorio.DocumentoEnUsoPorOtroAsync(propio.Id, null, null, null);

            // Assert
            enUso.Should().BeFalse();
        }

        [Test]
        public async Task DocumentoEnUsoPorOtroAsync_DocumentoDeClienteDeBaja_DevuelveTrue() {
            // Arrange
            await CrearCliente("a@correo.test", "12345678Z", baja: true);
            var otro = await CrearCliente("b@correo.test", "87654321X");

            // Act
            var enUso = await Repositorio.DocumentoEnUsoPorOtroAsync(otro.Id, "12345678Z", null, null);

            // Assert
            enUso.Should().BeTrue();
        }
    }

    [TestFixture]
    public class Add : ClienteRepositoryTests {
        [Test]
        public async Task AddAsync_ClienteNuevo_QuedaGuardadoEnLaBaseDeDatos() {
            // Arrange
            var nuevo = new Cliente {
                Nombre = "Lucía",
                Apellidos = "Navarro",
                Email = "lucia@correo.test",
                Telefono = "611222333",
                FechaNacimiento = new DateOnly(1995, 3, 21),
                Dni = "12345678Z"
            };

            // Act
            var guardado = await Repositorio.AddAsync(nuevo);

            // Assert
            await using var otroContexto = NuevoContexto();
            var leidos = await otroContexto.Clientes.Where(c => c.Id == guardado.Id).ToListAsync();
            leidos.Should().HaveCount(1);
            leidos[0].Email.Should().Be("lucia@correo.test");
            leidos[0].Activo.Should().BeTrue();
        }

        [Test]
        public async Task AddAsync_CorreoDuplicado_LaBaseDeDatosLoRechaza() {
            // Arrange
            await CrearCliente("ana@correo.test", "11111111H");
            var duplicado = new Cliente {
                Nombre = "Otra",
                Apellidos = "Persona",
                Email = "ana@correo.test",
                Telefono = "699000111",
                FechaNacimiento = new DateOnly(1990, 1, 1),
                Dni = "22222222J"
            };

            // Act
            Func<Task> accion = async () => await Repositorio.AddAsync(duplicado);

            // Assert
            await accion.Should().ThrowAsync<DbUpdateException>();
        }
    }

    [TestFixture]
    public class Update : ClienteRepositoryTests {
        [Test]
        public async Task UpdateAsync_ClienteExiste_CopiaLosDatosYNoCambiaElCorreo() {
            // Arrange
            var existente = await CrearCliente("ana@correo.test", "12345678Z");
            var datos = new Cliente {
                Id = existente.Id,
                Nombre = "Lucía",
                Apellidos = "Navarro Prieto",
                Email = "otro@correo.test",
                Telefono = "699111222",
                FechaNacimiento = new DateOnly(1995, 3, 21),
                Dni = "87654321X"
            };

            // Act
            var actualizado = await Repositorio.UpdateAsync(datos);

            // Assert
            actualizado.Should().NotBeNull();
            await using var otroContexto = NuevoContexto();
            var leidos = await otroContexto.Clientes.Where(c => c.Id == existente.Id).ToListAsync();
            leidos[0].Nombre.Should().Be("Lucía");
            leidos[0].Apellidos.Should().Be("Navarro Prieto");
            leidos[0].Telefono.Should().Be("699111222");
            leidos[0].Dni.Should().Be("87654321X");
            leidos[0].Email.Should().Be("ana@correo.test");
        }

        [Test]
        public async Task UpdateAsync_IdInexistente_DevuelveNull() {
            // Arrange
            var datos = new Cliente { Id = Guid.NewGuid(), Nombre = "Lucía", Apellidos = "Navarro" };

            // Act
            var actualizado = await Repositorio.UpdateAsync(datos);

            // Assert
            actualizado.Should().BeNull();
        }

        [Test]
        public async Task UpdateAsync_ClienteDeBaja_DevuelveNull() {
            // Arrange
            var existente = await CrearCliente(baja: true);
            var datos = new Cliente { Id = existente.Id, Nombre = "Lucía", Apellidos = "Navarro" };

            // Act
            var actualizado = await Repositorio.UpdateAsync(datos);

            // Assert
            actualizado.Should().BeNull();
        }
    }

    [TestFixture]
    public class Delete : ClienteRepositoryTests {
        [Test]
        public async Task DeleteAsync_ClienteExiste_LoDaDeBajaSinBorrarLaFila() {
            // Arrange
            var existente = await CrearCliente();

            // Act
            var borrado = await Repositorio.DeleteAsync(existente.Id);

            // Assert
            borrado.Should().BeTrue();
            await using var otroContexto = NuevoContexto();
            var leidos = await otroContexto.Clientes.Where(c => c.Id == existente.Id).ToListAsync();
            leidos.Should().HaveCount(1);
            leidos[0].IsDeleted.Should().BeTrue();
            leidos[0].DeletedAt.Should().NotBeNull();
            leidos[0].Activo.Should().BeFalse();
        }

        [Test]
        public async Task DeleteAsync_IdInexistente_DevuelveFalse() {
            // Arrange

            // Act
            var borrado = await Repositorio.DeleteAsync(Guid.NewGuid());

            // Assert
            borrado.Should().BeFalse();
        }

        [Test]
        public async Task DeleteAsync_ClienteYaDeBaja_DevuelveFalse() {
            // Arrange
            var existente = await CrearCliente(baja: true);

            // Act
            var borrado = await Repositorio.DeleteAsync(existente.Id);

            // Assert
            borrado.Should().BeFalse();
        }
    }
}