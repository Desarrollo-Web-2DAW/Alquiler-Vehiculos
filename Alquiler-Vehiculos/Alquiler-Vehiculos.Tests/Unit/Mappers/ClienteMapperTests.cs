using Alquiler_Vehiculos.Dto.Clientes;
using Alquiler_Vehiculos.Mappers;
using Alquiler_Vehiculos.Models.Usuarios.Clientes;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Mappers;

/// <summary>
/// Tests unitarios de las conversiones de cliente.
/// Requisitos cubiertos: RF-13, RF-14.
/// </summary>
[Category("Unit")]
public class ClienteMapperTests {
    [TestFixture]
    public class CasosPositivos {
        /// <summary>
        /// Dado un cliente con todos sus datos, cuando se convierte en DTO,
        /// entonces el DTO lleva los mismos datos.
        /// </summary>
        [Test]
        public void ToDto_ClienteCompleto_CopiaTodosLosDatos() {
            // Arrange
            var cliente = new Cliente {
                Nombre = "Lucía",
                Apellidos = "Navarro Prieto",
                Email = "lucia.navarro@correo.test",
                Telefono = "611222333",
                FechaNacimiento = new DateOnly(1995, 3, 21),
                Dni = "12345678Z",
                Nie = "X1234567L",
                Pasaporte = "AAA1234565",
                NumeroPermisoConducir = "B1234567",
                Activo = true
            };

            // Act
            var dto = cliente.ToDto();

            // Assert
            dto.Id.Should().Be(cliente.Id);
            dto.Nombre.Should().Be("Lucía");
            dto.Apellidos.Should().Be("Navarro Prieto");
            dto.Email.Should().Be("lucia.navarro@correo.test");
            dto.Telefono.Should().Be("611222333");
            dto.FechaNacimiento.Should().Be(new DateOnly(1995, 3, 21));
            dto.Dni.Should().Be("12345678Z");
            dto.Nie.Should().Be("X1234567L");
            dto.Pasaporte.Should().Be("AAA1234565");
            dto.NumeroPermisoConducir.Should().Be("B1234567");
            dto.Activo.Should().BeTrue();
            dto.DadoDeBaja.Should().BeFalse();
            dto.FechaBaja.Should().BeNull();
        }

        /// <summary>
        /// Dado un cliente con baja lógica, cuando se convierte en DTO,
        /// entonces el DTO indica la baja y su fecha.
        /// </summary>
        [Test]
        public void ToDto_ClienteDadoDeBaja_IndicaLaBajaYSuFecha() {
            // Arrange
            var baja = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
            var cliente = new Cliente { IsDeleted = true, DeletedAt = baja };

            // Act
            var dto = cliente.ToDto();

            // Assert
            dto.DadoDeBaja.Should().BeTrue();
            dto.FechaBaja.Should().Be(baja);
        }

        /// <summary>
        /// Dado un alta con espacios y mayúsculas sobrantes, cuando se convierte en modelo,
        /// entonces los datos quedan normalizados.
        /// </summary>
        [Test]
        public void ToModel_AltaConEspaciosYMayusculas_NormalizaLosDatos() {
            // Arrange
            var dto = new ClienteCreateDto {
                Nombre = "  Lucía ",
                Apellidos = " Navarro Prieto  ",
                Email = "  Lucia.Navarro@Correo.TEST ",
                Telefono = " 611222333 ",
                FechaNacimiento = new DateOnly(1995, 3, 21),
                Dni = " 12345678z ",
                Nie = "x1234567l",
                Pasaporte = " aaa1234565 ",
                NumeroPermisoConducir = " b1234567 "
            };

            // Act
            var cliente = dto.ToModel();

            // Assert
            cliente.Nombre.Should().Be("Lucía");
            cliente.Apellidos.Should().Be("Navarro Prieto");
            cliente.Email.Should().Be("lucia.navarro@correo.test");
            cliente.Telefono.Should().Be("611222333");
            cliente.Dni.Should().Be("12345678Z");
            cliente.Nie.Should().Be("X1234567L");
            cliente.Pasaporte.Should().Be("AAA1234565");
            cliente.NumeroPermisoConducir.Should().Be("B1234567");
        }

        /// <summary>
        /// Dada un alta, cuando se convierte en modelo, entonces el cliente nace activo
        /// y sin baja.
        /// </summary>
        [Test]
        public void ToModel_Alta_NaceActivoYSinBaja() {
            // Arrange
            var dto = new ClienteCreateDto { Nombre = "Lucía", Apellidos = "Navarro", Email = "a@b.es", Telefono = "611222333" };

            // Act
            var cliente = dto.ToModel();

            // Assert
            cliente.Activo.Should().BeTrue();
            cliente.IsDeleted.Should().BeFalse();
            cliente.DeletedAt.Should().BeNull();
        }

        /// <summary>
        /// Dada una modificación con espacios y mayúsculas sobrantes, cuando se convierte
        /// en modelo, entonces los datos quedan normalizados y no lleva correo.
        /// </summary>
        [Test]
        public void ToModel_ModificacionConEspaciosYMayusculas_NormalizaYNoLlevaCorreo() {
            // Arrange
            var dto = new ClienteUpdateDto {
                Nombre = "  Lucía Marina ",
                Apellidos = " Navarro Prieto ",
                Telefono = " +34 699 111 222 ",
                FechaNacimiento = new DateOnly(1995, 3, 21),
                Dni = " 87654321x "
            };

            // Act
            var cliente = dto.ToModel();

            // Assert
            cliente.Nombre.Should().Be("Lucía Marina");
            cliente.Apellidos.Should().Be("Navarro Prieto");
            cliente.Telefono.Should().Be("+34 699 111 222");
            cliente.Dni.Should().Be("87654321X");
            cliente.Email.Should().BeEmpty();
        }
    }

    [TestFixture]
    public class CasosNegativos {
        /// <summary>
        /// Dados documentos vacíos o en blanco, cuando se convierte el alta en modelo,
        /// entonces se guardan como nulos y no como texto vacío.
        /// </summary>
        [TestCase("")]
        [TestCase("   ")]
        public void ToModel_DocumentosEnBlanco_SeGuardanComoNulos(string valor) {
            // Arrange
            var dto = new ClienteCreateDto {
                Nombre = "Lucía",
                Apellidos = "Navarro",
                Email = "a@b.es",
                Telefono = "611222333",
                Dni = valor,
                Nie = valor,
                Pasaporte = valor,
                NumeroPermisoConducir = valor
            };

            // Act
            var cliente = dto.ToModel();

            // Assert
            cliente.Dni.Should().BeNull();
            cliente.Nie.Should().BeNull();
            cliente.Pasaporte.Should().BeNull();
            cliente.NumeroPermisoConducir.Should().BeNull();
        }

        /// <summary>
        /// Dados documentos nulos, cuando se convierte la modificación en modelo,
        /// entonces siguen siendo nulos.
        /// </summary>
        [Test]
        public void ToModel_ModificacionSinDocumentos_DejaLosDocumentosNulos() {
            // Arrange
            var dto = new ClienteUpdateDto { Nombre = "Lucía", Apellidos = "Navarro", Telefono = "611222333" };

            // Act
            var cliente = dto.ToModel();

            // Assert
            cliente.Dni.Should().BeNull();
            cliente.Nie.Should().BeNull();
            cliente.Pasaporte.Should().BeNull();
            cliente.NumeroPermisoConducir.Should().BeNull();
        }
    }
}