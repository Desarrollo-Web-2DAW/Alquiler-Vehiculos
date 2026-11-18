using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Storage;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Errors.Storage;

[TestFixture]
[Category("Unit")]
public class StorageErrorTests {
    [TestCase("vacio", "El archivo está vacío o no se ha proporcionado.")]
    [TestCase("grande", "El archivo supera el tamaño máximo permitido.")]
    [TestCase("extension", "La extensión del archivo no está permitida.")]
    [TestCase("contenido", "El tipo de contenido del archivo no está permitido.")]
    [TestCase("nombre", "El nombre del archivo contiene caracteres no válidos.")]
    public void Factoria_ErrorDeValidacion_DevuelveReglaDeNegocio(string caso, string mensaje) {
        // Arrange
        // Cada caso representa una validación de almacenamiento.

        // Act
        DomainError error = caso switch {
            "vacio" => StorageError.ArchivoVacio(),
            "grande" => StorageError.ArchivoMuyGrande(),
            "extension" => StorageError.ExtensionNoPermitida(),
            "contenido" => StorageError.TipoContenidoNoPermitido(),
            _ => StorageError.NombreArchivoInvalido()
        };

        // Assert
        error.Should().BeOfType<BusinessRuleError>();
        error.Message.Should().Be(mensaje);
    }

    [TestCase(true, "No se ha podido guardar el archivo en el servidor.")]
    [TestCase(false, "No se ha podido eliminar el archivo del servidor.")]
    public void Factoria_ErrorDeDisco_DevuelveErrorInterno(bool guardar, string mensaje) {
        // Arrange
        // No se necesita provocar un fallo real de disco para probar la factoría.

        // Act
        DomainError error = guardar ? StorageError.ErrorGuardando() : StorageError.ErrorEliminando();

        // Assert
        error.Should().BeOfType<InternalError>();
        error.Message.Should().Be(mensaje);
    }
}
