using Alquiler_Vehiculos.Data.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Alquiler_Vehiculos.Tests.Integration.Data.Vehiculo;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class MongoDbContextTests {
    private MongoDbContainer? _container;
    private MongoClient? _client;
    private IMongoDatabase _database = null!;
    private MongoDbContext _context = null!;
    private static readonly DateTime Fecha = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly ObjectId Id = ObjectId.Parse("000000000000000000000001");
    private static readonly ObjectId OtroId = ObjectId.Parse("000000000000000000000002");

    [OneTimeSetUp]
    public async Task ArrancarContenedor() {
        _container = new MongoDbBuilder("mongo:7.0").Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await _container.StartAsync(timeout.Token);
        _client = new MongoClient(_container.GetConnectionString());
        _database = _client.GetDatabase("catalogo_tests");
    }

    [SetUp]
    public async Task LimpiarDatosEIndices() {
        // Al recrear la colección no quedan datos ni índices de casos anteriores.
        await _database.DropCollectionAsync("marcas");
        await _database.CreateCollectionAsync("marcas");
        _context = new MongoDbContext(_database);
    }

    [OneTimeTearDown]
    public async Task EliminarContenedor() {
        try {
            _client?.Dispose();
        } finally {
            if (_container is not null) await _container.DisposeAsync();
        }
    }

    private static MarcaModel CrearMarca(ObjectId? id = null, string nombre = "Toyota") => new(
        id ?? Id, nombre, "Japón", "https://www.toyota.es", new List<string> { "Corolla", "Yaris" },
        false, Fecha, null, null);

    private IMongoCollection<MarcaModel> ColeccionDeLectura() =>
        new MongoDbContext(_database).Marcas;

    [Test]
    public void Marcas_ContextoConfigurado_UsaLaColeccionMarcasDeLaBaseIndicada() {
        // Arrange
        var contexto = _context;

        // Act
        var coleccion = contexto.Marcas;

        // Assert
        coleccion.CollectionNamespace.CollectionName.Should().Be("marcas");
        coleccion.CollectionNamespace.DatabaseNamespace.DatabaseName.Should().Be("catalogo_tests");
    }

    [Test]
    public async Task InsertOne_MarcaValida_PersisteDatosModelosYAuditoria() {
        // Arrange
        var marca = CrearMarca() with { IsDeleted = true, UpdatedAt = Fecha.AddDays(1), DeletedAt = Fecha.AddDays(2) };

        // Act
        await _context.Marcas.InsertOneAsync(marca);

        // Assert
        var guardada = await ColeccionDeLectura().Find(m => m.Id == marca.Id).SingleAsync();
        guardada.Should().BeEquivalentTo(marca);
        guardada.Should().NotBeSameAs(marca);
        var documento = await _database.GetCollection<BsonDocument>("marcas")
            .Find(new BsonDocument("_id", marca.Id)).SingleAsync();
        documento["_id"].AsObjectId.Should().Be(marca.Id);
        documento["ModelosDisponibles"].AsBsonArray.Should().HaveCount(2);
    }

    [Test]
    public async Task Find_MarcaInexistente_DevuelveNull() {
        // Arrange
        var coleccion = _context.Marcas;

        // Act
        var resultado = await coleccion.Find(m => m.Id == OtroId).FirstOrDefaultAsync();

        // Assert
        resultado.Should().BeNull();
    }

    [Test]
    public async Task ReplaceOne_MarcaExistente_PersisteCambios() {
        // Arrange
        var original = CrearMarca();
        await _context.Marcas.InsertOneAsync(original);
        var modificada = original with {
            Nombre = "Toyota actualizada", ModelosDisponibles = new List<string> { "Prius" },
            UpdatedAt = Fecha.AddDays(1)
        };

        // Act
        var resultado = await _context.Marcas.ReplaceOneAsync(m => m.Id == original.Id, modificada);

        // Assert
        resultado.ModifiedCount.Should().Be(1);
        var guardada = await ColeccionDeLectura().Find(m => m.Id == original.Id).SingleAsync();
        guardada.Should().BeEquivalentTo(modificada);
    }

    [Test]
    public async Task DeleteOne_MarcaExistente_EliminaDocumento() {
        // Arrange
        var marca = CrearMarca();
        await _context.Marcas.InsertOneAsync(marca);

        // Act
        var resultado = await _context.Marcas.DeleteOneAsync(m => m.Id == marca.Id);

        // Assert
        resultado.DeletedCount.Should().Be(1);
        (await ColeccionDeLectura().CountDocumentsAsync(FilterDefinition<MarcaModel>.Empty)).Should().Be(0);
    }

    [Test]
    public async Task InsertOne_IdDuplicado_RechazaDocumento() {
        // Arrange
        await _context.Marcas.InsertOneAsync(CrearMarca());
        var duplicada = CrearMarca(nombre: "Otra marca");

        // Act
        Func<Task> insertar = () => _context.Marcas.InsertOneAsync(duplicada);

        // Assert
        var error = await insertar.Should().ThrowAsync<MongoWriteException>();
        error.Which.WriteError.Category.Should().Be(ServerErrorCategory.DuplicateKey);
        var guardada = await ColeccionDeLectura().Find(m => m.Id == duplicada.Id).SingleAsync();
        guardada.Nombre.Should().Be("Toyota");
    }

    [Test]
    public async Task CrearIndices_SinIndiceDeNombre_CreaIndiceAscendente() {
        // Arrange
        var contexto = _context;

        // Act
        contexto.CrearIndices();

        // Assert
        using var cursor = await ColeccionDeLectura().Indexes.ListAsync();
        var indices = await cursor.ToListAsync();
        indices.Should().ContainSingle(i => i["name"].AsString == "Nombre_1");
        var indice = indices.Single(i => i["name"].AsString == "Nombre_1");
        indice["key"].AsBsonDocument.Should().Equal(new BsonDocument("Nombre", 1));
        indice.GetValue("unique", false).AsBoolean.Should().BeFalse();
    }

    [Test]
    public async Task CrearIndices_LlamarDosVeces_NoDuplicaIndiceNiBorraDatos() {
        // Arrange
        var marca = CrearMarca();
        await _context.Marcas.InsertOneAsync(marca);
        _context.CrearIndices();

        // Act
        _context.CrearIndices();

        // Assert
        using var cursor = await ColeccionDeLectura().Indexes.ListAsync();
        var indices = await cursor.ToListAsync();
        indices.Should().ContainSingle(i => i["name"].AsString == "Nombre_1");
        var guardada = await ColeccionDeLectura().Find(m => m.Id == marca.Id).SingleAsync();
        guardada.Should().BeEquivalentTo(marca);
    }

    [Test]
    public async Task InsertMany_NombreRepetidoConIdsDistintos_IndicePermiteAmbasMarcas() {
        // Arrange
        _context.CrearIndices();
        var marcas = new[] { CrearMarca(Id), CrearMarca(OtroId) };

        // Act
        await _context.Marcas.InsertManyAsync(marcas);

        // Assert
        var guardadas = await ColeccionDeLectura().Find(m => m.Nombre == "Toyota").ToListAsync();
        guardadas.Select(m => m.Id).Should().BeEquivalentTo(new[] { Id, OtroId });
    }
}
