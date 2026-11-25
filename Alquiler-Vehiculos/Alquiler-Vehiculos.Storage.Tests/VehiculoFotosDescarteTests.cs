using Alquiler_Vehiculos.Cache;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Repositories.Vehiculo.Concesionario;
using Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;
using Alquiler_Vehiculos.Repositories.Vehiculo.Marca;
using Alquiler_Vehiculos.Services.Reserva;
using Alquiler_Vehiculos.Services.Taller;
using Alquiler_Vehiculos.Services.Vehiculo;
using Alquiler_Vehiculos.Services.Vehiculo.Transactions;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Query;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Vehiculo;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using MongoDB.Bson;

namespace Alquiler_Vehiculos.Storage.Tests;

[TestFixture]
[Category("Integration")]
public class VehiculoFotosDescarteTests : StorageTestBase {
    [TestCase(false)]
    [TestCase(true)]
    public async Task Update_FallaLaTransaccion_EliminaFotoPreparadaYNoPersisteMetadatos(bool excepcion) {
        var fotoService = CrearFotoService();
        var transacciones = new Mock<IVehiculoTransactionsService>();
        if (excepcion) {
            transacciones
                .Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<VehiculoModel>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Fallo controlado"));
        } else {
            transacciones
                .Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<VehiculoModel>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<UpdateVehiculoResult, DomainError>(
                    ValidationError.Create("Actualización rechazada")));
        }

        var concesionario = new Mock<IConcesionarioRepository>();
        concesionario.Setup(r => r.GetById(1))
            .ReturnsAsync(new ConcesionarioModel(1, "Concesionario pruebas", "Leganés", "912345678",
                "pruebas@example.test", false, DateTime.UtcNow, null, null));
        var marcas = new Mock<IMarcaRepository>();
        marcas.Setup(r => r.GetAll()).ReturnsAsync([
            new MarcaModel(ObjectId.Empty, "Toyota", "Japón", "https://toyota.example",
                ["Corolla"], false, DateTime.UtcNow, null, null)
        ]);
        var service = new VehiculoService(
            new Mock<IVehiculoRepository>().Object,
            fotoService,
            transacciones.Object,
            new Mock<ICacheService>().Object,
            new CreateVehiculoValidator(),
            new UpdateVehiculoValidator(),
            concesionario.Object,
            new QueryRequestVehiculoValidator(),
            marcas.Object,
            new Mock<IParteTallerService>().Object);

        using var contenido = new MemoryStream([1, 2, 3, 4]);
        var archivo = new FormFile(contenido, 0, contenido.Length, "foto", "nueva.jpg") {
            Headers = new HeaderDictionary(), ContentType = "image/jpeg"
        };
        var dto = new UpdateVehiculoDto {
            Foto = archivo,
            Matricula = "1234BCD",
            Marca = "Toyota",
            Modelo = "Corolla",
            Acriss = "CCMD",
            Color = "Rojo",
            PrecioBaseDia = 55,
            Plazas = 5,
            MaleteroLitros = 470,
            AutonomiaKm = 900,
            Kilometraje = 1000,
            FechaUltimaRevision = "2026-01-15",
            Concesionario = 1
        };

        if (excepcion) {
            var actuar = () => service.Update(dto, "vehiculo-1");
            await actuar.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Fallo controlado");
        } else {
            var resultado = await service.Update(dto, "vehiculo-1");
            resultado.IsFailure.Should().BeTrue();
        }

        transacciones.Verify(s => s.UpdateAsync("vehiculo-1", It.IsAny<VehiculoModel>(),
            It.IsAny<CancellationToken>()), Times.Once);
        Directory.GetFiles(Path.Combine(RootPath, "uploads"), "*", SearchOption.AllDirectories)
            .Should().BeEmpty();
        await using var lectura = CrearContextoDeLectura();
        (await lectura.Fotos.CountAsync()).Should().Be(0);
    }
}
