using Alquiler_Vehiculos.Controllers.Vehiculo;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Services.Storage;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Alquiler_Vehiculos.Storage.Tests;

[TestFixture]
[Category("Integration")]
public class FotosArchivoControllerTests : StorageTestBase {
    [Test]
    public async Task GetArchivoHistorico_FotoDadaDeBaja_ConservaContenidoSinAccesoPublico() {
        // Arrange
        var storage = CrearServicio();
        var service = new FotoService(new FotoRepository(Db, NullLogger<FotoRepository>.Instance), storage);
        var controller = new FotosController(service, storage);
        byte[] datos = [1, 2, 3, 4];
        using var contenido = new MemoryStream(datos);
        var archivo = new FormFile(contenido, 0, contenido.Length, "archivo", "foto.jpg") {
            Headers = new HeaderDictionary(), ContentType = "image/jpeg"
        };
        var creada = await service.Create(archivo);
        creada.IsSuccess.Should().BeTrue();
        (await service.SoftDelete(creada.Value.Nombre)).IsSuccess.Should().BeTrue();

        // Act
        var publico = await controller.GetArchivo(creada.Value.Nombre);
        var historico = await controller.GetArchivoHistorico(creada.Value.Nombre);

        // Assert
        publico.Should().BeOfType<NotFoundObjectResult>();
        var respuesta = historico.Should().BeOfType<FileStreamResult>().Subject;
        await using var stream = respuesta.FileStream;
        using var copia = new MemoryStream();
        await stream.CopyToAsync(copia);
        copia.ToArray().Should().Equal(datos);
        respuesta.ContentType.Should().Be("image/jpeg");
        await using var lectura = CrearContextoDeLectura();
        (await lectura.Fotos.SingleAsync()).Activo.Should().BeFalse();
    }

    [TestCase("coche.jpg", "image/jpeg")]
    [TestCase("coche.png", "image/png")]
    [TestCase("coche.sinmime", "application/octet-stream")]
    public async Task GetArchivo_FotoPersistida_DevuelveContenidoYTipoMime(string nombre, string mime) {
        // Arrange
        var storage = CrearServicio(Configuracion(new() {
            ["Storage:AllowedExtensions:0"] = Path.GetExtension(nombre),
            ["Storage:AllowedContentTypes:0"] = mime
        }));
        var service = new FotoService(new FotoRepository(Db, NullLogger<FotoRepository>.Instance), storage);
        var controller = new FotosController(service, storage);
        byte[] datos = [1, 2, 3, 4];
        using var contenido = new MemoryStream(datos);
        var archivo = new FormFile(contenido, 0, contenido.Length, "archivo", nombre) {
            Headers = new HeaderDictionary(), ContentType = mime
        };
        var creada = await service.Create(archivo);
        creada.IsSuccess.Should().BeTrue();

        // Act
        var resultado = await controller.GetArchivo(creada.Value.Nombre);

        // Assert
        var respuesta = resultado.Should().BeOfType<FileStreamResult>().Subject;
        respuesta.ContentType.Should().Be(mime);
        await using var stream = respuesta.FileStream;
        using var copia = new MemoryStream();
        await stream.CopyToAsync(copia);
        copia.ToArray().Should().Equal(datos);
        File.Exists(storage.GetFullPath(Path.Combine("vehiculos", creada.Value.Nombre))).Should().BeTrue();
    }

    [TestCase("invalida", 400)]
    [TestCase("ausente", 404)]
    [TestCase("sinFichero", 404)]
    public async Task GetArchivo_SinImagenDisponible_DevuelveErrorSinStream(string caso, int estado) {
        // Arrange
        var storage = CrearServicio();
        var service = new FotoService(new FotoRepository(Db, NullLogger<FotoRepository>.Instance), storage);
        var controller = new FotosController(service, storage);
        var nombre = caso == "invalida" ? ".." : "ausente.jpg";
        if (caso == "sinFichero") {
            Db.Fotos.Add(new Foto { Nombre = nombre, Ruta = "/uploads/vehiculos/ausente.jpg", Extension = ".jpg", Tamano = 4, Activo = true });
            await Db.SaveChangesAsync();
        }

        // Act
        var resultado = await controller.GetArchivo(nombre);

        // Assert
        var respuesta = resultado.Should().BeAssignableTo<ObjectResult>().Subject;
        respuesta.StatusCode.Should().Be(estado);
        if (estado == 404) respuesta.Value.Should().BeEquivalentTo(new {
            code = "FOTO_NO_ENCONTRADA", message = $"No se ha encontrado la foto con el nombre:{nombre}"
        });
        else respuesta.Value.Should().BeEquivalentTo(new {
            code = "VALIDACION", message = "El identificador debe ser un nombre de foto, no una ruta.",
            details = new Dictionary<string, string[]>()
        });
    }
}
