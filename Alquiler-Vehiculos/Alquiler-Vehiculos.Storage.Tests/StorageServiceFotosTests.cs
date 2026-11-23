using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Storage;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Alquiler_Vehiculos.Storage.Tests;

public partial class StorageServiceTests {
    [Test]
    public async Task DescartarFotoAsync_SubidaSinVehiculo_DesactivaYConservaBytes() {
        // Arrange
        var storage = CrearServicio();
        byte[] datos = [1, 2, 3];
        using var contenido = new MemoryStream(datos);
        var subida = await storage.SaveFileAsync(Archivo(contenido), "vehiculos");
        subida.IsSuccess.Should().BeTrue();
        var nombre = subida.Value;

        // Act
        var resultado = await storage.DescartarFotoAsync(nombre);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        await using var lectura = CrearContextoDeLectura();
        var foto = await lectura.Fotos.SingleAsync(f => f.Nombre == nombre);
        foto.Activo.Should().BeFalse();
        foto.DeletedAt.Should().NotBeNull();
        File.ReadAllBytes(Path.Combine(RootPath, "uploads", "vehiculos", nombre)).Should().Equal(datos);
    }

    [Test]
    public async Task SaveFileAsync_FallaLaCopia_NoDejaFotoActiva() {
        // Arrange
        var storage = CrearServicio();
        var archivo = new Mock<IFormFile>();
        archivo.SetupGet(f => f.Length).Returns(1);
        archivo.SetupGet(f => f.FileName).Returns("coche.jpg");
        archivo.SetupGet(f => f.ContentType).Returns("image/jpeg");
        archivo.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Copia interrumpida"));

        // Act
        var resultado = await storage.SaveFileAsync(archivo.Object, "vehiculos");

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(StorageError.ErrorGuardando());
        await using var lectura = CrearContextoDeLectura();
        (await lectura.Fotos.CountAsync(f => f.Activo)).Should().Be(0);
        archivo.VerifyGet(f => f.Length, Times.AtLeastOnce);
        archivo.VerifyGet(f => f.FileName, Times.AtLeastOnce);
        archivo.VerifyGet(f => f.ContentType, Times.Once);
        archivo.Verify(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task SaveFileAsync_ArchivoNuloOVacio_Devuelve400SinEscribir(bool nulo) {
        // Arrange
        var storage = CrearServicio();
        using var contenido = new MemoryStream();
        var archivo = nulo ? null : Archivo(contenido);

        // Act
        var resultado = await storage.SaveFileAsync(archivo!, "vehiculos");

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>();
        await using var lectura = CrearContextoDeLectura();
        (await lectura.Fotos.CountAsync()).Should().Be(0);
        Directory.GetFiles(Path.Combine(RootPath, "uploads"), "*", SearchOption.AllDirectories).Should().BeEmpty();
    }
}
