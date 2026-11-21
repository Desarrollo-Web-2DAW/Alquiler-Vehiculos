using Alquiler_Vehiculos.Data.Seed.Vehiculo;
using Alquiler_Vehiculos.Data.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Data.Seed.Vehiculo;

[TestFixture]
[Category("Unit")]
public class MarcaSeederTests {
    private static readonly DateTime Fecha = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void BuildMarcas_FechaFija_DevuelveOchoMarcasConIdentificadoresEstables() {
        // Arrange
        string[] nombres = ["Toyota", "Renault", "Nissan", "Jeep", "Volkswagen", "BMW", "MINI", "Ford"];

        // Act
        var marcas = MarcaSeeder.BuildMarcas(Fecha);
        var segundaConstruccion = MarcaSeeder.BuildMarcas(Fecha.AddDays(1));

        // Assert
        marcas.Select(m => m.Nombre).Should().Equal(nombres);
        marcas.Select(m => m.Id).Should().OnlyHaveUniqueItems().And.NotContain(ObjectId.Empty);
        segundaConstruccion.Select(m => m.Id).Should().Equal(marcas.Select(m => m.Id));
        marcas.Should().OnlyContain(m => m.Pais.Length > 0 && m.Web.StartsWith("https://") && m.ModelosDisponibles.Count > 0);
    }

    [Test]
    public void BuildMarcas_FlotaInicial_ContieneLosNueveModelosSinMarcasHuerfanas() {
        // Arrange
        var vehiculos = VehiculoSeeder.BuildVehiculos(Fecha);

        // Act
        var marcas = MarcaSeeder.BuildMarcas(Fecha);

        // Assert
        marcas.Sum(m => m.ModelosDisponibles.Count).Should().Be(9);
        foreach (var vehiculo in vehiculos) {
            marcas.Should().ContainSingle(m => m.Nombre == vehiculo.Marca && m.ModelosDisponibles.Contains(vehiculo.Modelo));
        }
        marcas.Should().OnlyContain(m => vehiculos.Any(v => v.Marca == m.Nombre));
        marcas.Where(m => m.IsDeleted).Should().ContainSingle().Which.Nombre.Should().Be("MINI");
    }

    [Test]
    public void BuildMarcas_FechaFija_ConservaAuditoriaDeActivasYDesactivada() {
        // Arrange
        var fechaBaja = Fecha;

        // Act
        var marcas = MarcaSeeder.BuildMarcas(fechaBaja);

        // Assert
        marcas.Where(m => !m.IsDeleted).Should().OnlyContain(m => m.CreatedAt == fechaBaja && m.UpdatedAt == null && m.DeletedAt == null);
        var mini = marcas.Single(m => m.IsDeleted);
        mini.CreatedAt.Should().Be(fechaBaja.AddMonths(-6));
        mini.UpdatedAt.Should().Be(fechaBaja);
        mini.DeletedAt.Should().Be(fechaBaja);
    }

    [Test]
    public void BuildMarcas_DosConstrucciones_NoComparteListasDeModelos() {
        // Arrange
        var primera = MarcaSeeder.BuildMarcas(Fecha);

        // Act
        var segunda = MarcaSeeder.BuildMarcas(Fecha);
        primera[0].ModelosDisponibles.Clear();

        // Assert
        segunda[0].ModelosDisponibles.Should().Equal("Corolla Sedán");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SeedAsync_ConYSinDatos_InsertaSoloCuandoLaColeccionEstaVacia(bool existenDatos) {
        // Arrange
        var database = new Mock<IMongoDatabase>();
        var collection = new Mock<IMongoCollection<MarcaModel>>(MockBehavior.Strict);
        // AnyAsync optimiza la consulta proyectando BSON, no devuelve MarcaModel completos.
        var cursor = new Mock<IAsyncCursor<BsonDocument>>();
        var existente = MarcaSeeder.BuildMarcas(Fecha).Single(m => m.IsDeleted);
        cursor.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        cursor.SetupGet(c => c.Current).Returns(existenDatos
            ? new[] { new BsonDocument { { "_id", existente.Id }, { "IsDeleted", true } } }
            : Array.Empty<BsonDocument>());
        collection.Setup(c => c.FindAsync(It.IsAny<FilterDefinition<MarcaModel>>(),
                It.IsAny<FindOptions<MarcaModel, BsonDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursor.Object);
        collection.Setup(c => c.InsertManyAsync(It.IsAny<IEnumerable<MarcaModel>>(),
                It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        database.Setup(d => d.GetCollection<MarcaModel>("marcas", It.IsAny<MongoCollectionSettings>()))
            .Returns(collection.Object);
        var context = new MongoDbContext(database.Object);

        // Act
        await MarcaSeeder.SeedAsync(context);

        // Assert
        database.Verify(d => d.GetCollection<MarcaModel>("marcas", It.IsAny<MongoCollectionSettings>()),
            Times.Exactly(existenDatos ? 1 : 2));
        collection.Verify(c => c.FindAsync(It.IsAny<FilterDefinition<MarcaModel>>(),
            It.IsAny<FindOptions<MarcaModel, BsonDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        cursor.Verify(c => c.MoveNextAsync(It.IsAny<CancellationToken>()), Times.Once);
        cursor.VerifyGet(c => c.Current, Times.Once);
        collection.Verify(c => c.InsertManyAsync(It.Is<IEnumerable<MarcaModel>>(m => m.Count() == 8 &&
            m.Any(marca => marca.Nombre == "Renault" && marca.ModelosDisponibles.Contains("Megane"))),
            It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>()), existenDatos ? Times.Never() : Times.Once());
        collection.Verify(c => c.DeleteManyAsync(It.IsAny<FilterDefinition<MarcaModel>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
