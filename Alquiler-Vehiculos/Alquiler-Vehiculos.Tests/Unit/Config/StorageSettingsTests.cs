using Alquiler_Vehiculos.Config;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Config;

[TestFixture]
[Category("Unit")]
public class StorageSettingsTests {
    [Test]
    public void Constructor_SinConfiguracion_InicializaValoresPorDefecto() {
        // Arrange
        const string seccion = "Storage";

        // Act
        var settings = new StorageSettings();

        // Assert
        StorageSettings.SectionName.Should().Be(seccion);
        settings.UploadPath.Should().BeEmpty();
        settings.MaxFileSize.Should().BeEmpty();
        settings.AllowedExtensions.Should().BeEmpty();
        settings.AllowedContentTypes.Should().BeEmpty();
        settings.DefaultImage.Should().BeEmpty();
        settings.Folders.Should().BeEmpty();
    }

    [Test]
    public void Propiedades_ValoresAsignados_ConservaConfiguracion() {
        // Arrange
        var carpetas = new Dictionary<string, string> { ["Vehiculos"] = "vehiculos" };

        // Act
        var settings = new StorageSettings {
            UploadPath = "imagenes", MaxFileSize = "1024", AllowedExtensions = ".png",
            AllowedContentTypes = "image/png", DefaultImage = "default.png", Folders = carpetas
        };

        // Assert
        settings.UploadPath.Should().Be("imagenes");
        settings.MaxFileSize.Should().Be("1024");
        settings.AllowedExtensions.Should().Be(".png");
        settings.AllowedContentTypes.Should().Be("image/png");
        settings.DefaultImage.Should().Be("default.png");
        settings.Folders.Should().BeSameAs(carpetas);
    }

    [Test]
    public void Folders_ClaveConDistintasMayusculas_EncuentraMismaCarpeta() {
        // Arrange
        var settings = new StorageSettings();
        settings.Folders.Add("Vehiculos", "coches");

        // Act
        var carpeta = settings.Folders["VEHICULOS"];

        // Assert
        carpeta.Should().Be("coches");
        settings.Folders.Should().ContainSingle();
    }

    [Test]
    public void Constructor_DosInstancias_NoCompartenCarpetas() {
        // Arrange
        var primera = new StorageSettings();
        var segunda = new StorageSettings();

        // Act
        primera.Folders.Add("Vehiculos", "vehiculos");

        // Assert
        segunda.Folders.Should().BeEmpty();
    }
}
