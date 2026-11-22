using Alquiler_Vehiculos.Data.Seed.Storage;
using Alquiler_Vehiculos.Data.Seed.Vehiculo;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Data.Seed.Storage;

[TestFixture]
[Category("Unit")]
public class FotoSeederTests {
    private static readonly DateTime Fecha = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    [TestCase("uploads")]
    [TestCase("imagenes")]
    public void BuildFotos_ArchivosExistentes_DevuelveUnaFotoPorVehiculoConTamanoReal(string carpeta) {
        // Arrange
        var archivos = new Mock<IFileProvider>();
        var matriculas = VehiculoSeeder.BuildVehiculos(Fecha).Select(v => v.Matrícula).ToArray();
        for (var i = 0; i < matriculas.Length; i++) {
            var nombre = $"seed-{matriculas[i]}.jpg";
            archivos.Setup(a => a.GetFileInfo($"{carpeta}/vehiculos/{nombre}"))
                .Returns(new ArchivoSemilla(nombre, i + 100));
        }

        // Act
        var fotos = FotoSeeder.BuildFotos(archivos.Object, carpeta, Fecha);

        // Assert
        fotos.Should().HaveCount(12);
        fotos.Select(f => f.Ruta).Should().OnlyHaveUniqueItems();
        for (var i = 0; i < matriculas.Length; i++) {
            var nombre = $"seed-{matriculas[i]}.jpg";
            var foto = fotos.Should().ContainSingle(f => f.Nombre == nombre).Which;
            foto.Ruta.Should().Be($"/{carpeta}/vehiculos/{nombre}");
            foto.Tamano.Should().Be(i + 100);
            archivos.Verify(a => a.GetFileInfo($"{carpeta}/vehiculos/{nombre}"), Times.Once);
        }
        fotos.Should().OnlyContain(f => f.Activo && f.Extension == ".jpg" && f.CreatedAt == Fecha &&
            f.UpdatedAt == null && f.DeletedAt == null);
        archivos.Verify(a => a.GetFileInfo(It.IsAny<string>()), Times.Exactly(12));
        archivos.Verify(a => a.GetDirectoryContents(It.IsAny<string>()), Times.Never);
    }

    [TestCase(false, 0)]
    [TestCase(true, 0)]
    public void BuildFotos_ImagenAusenteOVacia_RechazaLaSemillaSinLeerLasSiguientes(bool existe, long tamano) {
        // Arrange
        var archivos = new Mock<IFileProvider>();
        IFileInfo archivo = existe ? new ArchivoSemilla("seed-1234BCD.jpg", tamano) : new NotFoundFileInfo("seed-1234BCD.jpg");
        archivos.Setup(a => a.GetFileInfo("uploads/vehiculos/seed-1234BCD.jpg")).Returns(archivo);

        // Act
        Action act = () => FotoSeeder.BuildFotos(archivos.Object, "uploads", Fecha);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*seed-1234BCD.jpg*Prepara primero el almacenamiento.*");
        archivos.Verify(a => a.GetFileInfo("uploads/vehiculos/seed-1234BCD.jpg"), Times.Once);
        archivos.Verify(a => a.GetFileInfo("uploads/vehiculos/seed-2345CDF.jpg"), Times.Never);
        archivos.Verify(a => a.GetFileInfo(It.IsAny<string>()), Times.Once);
    }

    // Datos de archivo en memoria: no se crean directorios, JPG ni streams físicos.
    private sealed class ArchivoSemilla(string nombre, long tamano) : IFileInfo {
        public bool Exists => true;
        public long Length => tamano;
        public string? PhysicalPath => null;
        public string Name => nombre;
        public DateTimeOffset LastModified => new(Fecha);
        public bool IsDirectory => false;
        public Stream CreateReadStream() => throw new InvalidOperationException("El seeder no debe leer el contenido de la imagen.");
    }
}