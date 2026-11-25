using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Repositories.Storage;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Alquiler_Vehiculos.Storage.Tests;

public partial class StorageServiceTests {
    [Test]
    public async Task PrepareAsync_GuardaBytesSinInsertarFotoYDeletePreparedLosElimina() {
        var service = CrearFotoService();
        byte[] datos = [1, 2, 3];
        using var contenido = new MemoryStream(datos);
        var archivo = new FormFile(contenido, 0, contenido.Length, "archivo", "foto.jpg") {
            Headers = new HeaderDictionary(), ContentType = "image/jpeg"
        };

        var preparada = await service.PrepareAsync(archivo);

        preparada.IsSuccess.Should().BeTrue();
        var ruta = Path.Combine(RootPath, "uploads", "vehiculos", preparada.Value.Nombre);
        File.ReadAllBytes(ruta).Should().Equal(datos);
        await using (var lectura = CrearContextoDeLectura())
            (await lectura.Fotos.CountAsync()).Should().Be(0);

        var borrado = await service.DeletePreparedFileAsync(preparada.Value.Nombre);

        borrado.IsSuccess.Should().BeTrue();
        File.Exists(ruta).Should().BeFalse();
    }

    [Test]
    public async Task PrepareAsync_ArchivoNoValido_NoEscribeEnDisco() {
        var service = CrearFotoService();
        using var contenido = new MemoryStream([1]);
        var archivo = new FormFile(contenido, 0, contenido.Length, "archivo", "foto.pdf") {
            Headers = new HeaderDictionary(), ContentType = "application/pdf"
        };

        var resultado = await service.PrepareAsync(archivo);

        resultado.IsFailure.Should().BeTrue();
        Directory.GetFileSystemEntries(Path.Combine(RootPath, "uploads")).Should().BeEmpty();
    }

    [Test]
    public async Task Create_FallaLaInsercionDeMetadatos_EliminaElArchivoFisico() {
        var repository = new Mock<IFotoRepository>();
        repository.Setup(r => r.Create(It.IsAny<Foto>()))
            .ThrowsAsync(new InvalidOperationException("Fallo controlado"));
        var service = CrearFotoService(repository: repository.Object);
        using var contenido = new MemoryStream([1, 2, 3]);
        var archivo = new FormFile(contenido, 0, contenido.Length, "archivo", "foto.jpg") {
            Headers = new HeaderDictionary(), ContentType = "image/jpeg"
        };

        var resultado = await service.Create(archivo);

        resultado.IsFailure.Should().BeTrue();
        Directory.GetFiles(Path.Combine(RootPath, "uploads"), "*", SearchOption.AllDirectories).Should().BeEmpty();
        repository.Verify(r => r.Create(It.IsAny<Foto>()), Times.Once);
    }
}
