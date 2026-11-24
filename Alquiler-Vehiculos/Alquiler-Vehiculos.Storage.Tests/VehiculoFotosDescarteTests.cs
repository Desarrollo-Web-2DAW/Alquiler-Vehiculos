using Alquiler_Vehiculos.Controllers.Vehiculo;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Services.Vehiculo;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Alquiler_Vehiculos.Storage.Tests;

// Integración de controlador, almacenamiento, repositorio y PostgreSQL reales.
// Solo se aísla el servicio de vehículos para provocar un rechazo o una excepción.
[TestFixture]
[Category("Integration")]
public class VehiculoFotosDescarteTests : StorageTestBase {
    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task GuardarVehiculo_FallaOperacion_DescartaSoloSubidaNuevaYConservaBytes(bool actualizar, bool excepcion) {
        // Arrange
        var storage = CrearServicio();
        byte[] originales = [1, 2, 3, 4];
        using var contenidoOriginal = new MemoryStream(originales);
        var subidaOriginal = await storage.SaveFileAsync(Archivo(contenidoOriginal, "original.jpg"), "vehiculos");
        subidaOriginal.IsSuccess.Should().BeTrue();
        var concesionario = new ConcesionarioModel(0, "Concesionario pruebas", "Leganés", "912345678",
            "pruebas@example.test", false, DateTime.UtcNow, null, null);
        Db.Concesionarios.Add(concesionario);
        await Db.SaveChangesAsync();
        var original = new VehiculoModel {
            FotoFk = subidaOriginal.Value, Matrícula = "1234BCD", Marca = "Marca pruebas",
            Modelo = "Modelo pruebas", Acriss = "ILAH", PrecioBaseDia = 55, Plazas = 5,
            MaleteroLitros = 470, AutonomiaKm = 900, Kilometraje = 1000,
            FechaUltimaRevision = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            ConesionarioFk = concesionario.Id
        };
        Db.Vehiculos.Add(original);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        var antes = await Db.Vehiculos.AsNoTracking().SingleAsync();
        var servicio = new Mock<IVehiculoService>();
        var error = ValidationError.WithFieldErrors(new Dictionary<string, string[]> {
            ["PrecioBaseDia"] = ["El precio base por día debe ser mayor que cero."]
        });
        if (actualizar) {
            var llamada = servicio.Setup(s => s.Update(It.IsAny<UpdateVehiculoDto>(), original.Id));
            if (excepcion) llamada.ThrowsAsync(new InvalidOperationException("Fallo controlado de integración"));
            else llamada.ReturnsAsync(Result.Failure<VehiculoDto, DomainError>(error));
        } else {
            var llamada = servicio.Setup(s => s.Create(It.IsAny<CreateVehiculoDto>()));
            if (excepcion) llamada.ThrowsAsync(new InvalidOperationException("Fallo controlado de integración"));
            else llamada.ReturnsAsync(Result.Failure<VehiculoDto, DomainError>(error));
        }
        var controller = new VehiculoController(servicio.Object, storage);
        byte[] nuevos = [9, 8, 7, 6, 5];
        using var contenidoNuevo = new MemoryStream(nuevos);
        var archivo = Archivo(contenidoNuevo, "nueva.jpg");
        ActionResult<VehiculoDto>? respuesta = null;

        // Act
        Func<Task> actuar = async () => {
            respuesta = actualizar
                ? await controller.Update(original.Id, archivo, "1234BCD", "Marca pruebas", "Modelo pruebas",
                    "ILAH", "Blanco", 0, 5, 470, 900, 1000, "2026-01-15", concesionario.Id)
                : await controller.Create(archivo, "9876BCD", "Marca pruebas", "Modelo pruebas",
                    "ILAH", "Blanco", 0, 5, 470, 900, 1000, "2026-01-15", concesionario.Id);
        };
        if (!excepcion) await actuar();

        // Assert
        if (excepcion) await actuar.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Fallo controlado de integración");
        else respuesta!.Result.Should().BeOfType<BadRequestObjectResult>();
        await using var lectura = CrearContextoDeLectura();
        var fotos = await lectura.Fotos.AsNoTracking().ToListAsync();
        fotos.Should().HaveCount(2);
        var descartada = fotos.Single(f => f.Nombre != subidaOriginal.Value);
        descartada.Activo.Should().BeFalse();
        descartada.DeletedAt.Should().NotBeNull();
        descartada.UpdatedAt.Should().NotBeNull();
        descartada.Tamano.Should().Be(nuevos.Length);
        File.ReadAllBytes(storage.GetFullPath(Path.Combine("vehiculos", descartada.Nombre))).Should().Equal(nuevos);
        var anterior = fotos.Single(f => f.Nombre == subidaOriginal.Value);
        anterior.Activo.Should().BeTrue();
        anterior.DeletedAt.Should().BeNull();
        File.ReadAllBytes(storage.GetFullPath(Path.Combine("vehiculos", anterior.Nombre))).Should().Equal(originales);
        var persistido = await lectura.Vehiculos.AsNoTracking().SingleAsync();
        persistido.Should().BeEquivalentTo(antes, o => o.Excluding(v => v.Foto));
        servicio.Verify(s => s.Create(It.Is<CreateVehiculoDto>(v => v.Foto == descartada.Nombre && v.PrecioBaseDia == 0)),
            actualizar ? Times.Never() : Times.Once());
        servicio.Verify(s => s.Update(It.Is<UpdateVehiculoDto>(v => v.Foto == descartada.Nombre && v.PrecioBaseDia == 0), original.Id),
            actualizar ? Times.Once() : Times.Never());
    }

    private static FormFile Archivo(Stream contenido, string nombre) =>
        new(contenido, 0, contenido.Length, "foto", nombre) {
            Headers = new HeaderDictionary(), ContentType = "image/jpeg"
        };
}
