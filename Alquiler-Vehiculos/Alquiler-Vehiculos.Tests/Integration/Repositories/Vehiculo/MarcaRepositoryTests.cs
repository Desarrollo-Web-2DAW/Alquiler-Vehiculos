using Alquiler_Vehiculos.Data.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Repositories.Vehiculo.Marca;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Alquiler_Vehiculos.Tests.Integration.Repositories.Vehiculo;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class MarcaRepositoryTests {
    [TestCase("no-es-un-objectid")]
    [TestCase("")]
    [TestCase("00000000000000000000000Z")]
    public async Task ConsultarOEliminar_IdMalFormado_DevuelveAusenciaSinModificarDatos(string id) {
        // Arrange
        var original = CrearMarca();
        await _context.Marcas.InsertOneAsync(original);

        // Act
        var encontrada = await _repository.GetById(id);
        var existe = await _repository.ExistById(id);
        var eliminada = await _repository.SoftDelete(id);
        var actualizada = await _repository.Update(id, CrearMarca(nombre: "No guardar"));

        // Assert
        encontrada.Should().BeNull();
        existe.Should().BeFalse();
        eliminada.Should().BeFalse();
        actualizada.Should().BeNull();
        var lectura = new MongoDbContext(_database);
        var marcas = await lectura.Marcas.Find(_ => true).ToListAsync();
        marcas.Should().ContainSingle().Which.Should().BeEquivalentTo(original);
    }

    private MongoDbContainer? _container;
    private MongoClient? _client;
    private IMongoDatabase _database = null!;
    private MongoDbContext _context = null!;
    private IMarcaRepository _repository = null!;
    private static readonly DateTime Fecha = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly ObjectId Id = ObjectId.Parse("000000000000000000000001");
    private static readonly ObjectId OtroId = ObjectId.Parse("000000000000000000000002");
    private static readonly ObjectId TercerId = ObjectId.Parse("000000000000000000000003");

    [OneTimeSetUp]
    public async Task ArrancarContenedor() {
        _container = new MongoDbBuilder("mongo:7.0").Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await _container.StartAsync(timeout.Token);
        _client = new MongoClient(_container.GetConnectionString());
        _database = _client.GetDatabase("marcas_repository_tests");
    }

    [SetUp]
    public async Task PrepararRepositorio() {
        await _database.DropCollectionAsync("marcas");
        _context = new MongoDbContext(_database);
        _context.CrearIndices();
        _repository = new MarcaRepository(_context, NullLogger<MarcaRepository>.Instance);
    }

    [OneTimeTearDown]
    public async Task EliminarContenedor() {
        try {
            _client?.Dispose();
        } finally {
            if (_container is not null) await _container.DisposeAsync();
        }
    }

    private static MarcaModel CrearMarca(ObjectId? id = null, string nombre = "Toyota", bool eliminada = false) =>
        new(id ?? Id, nombre, "Japón", "https://www.toyota.es", new List<string> { "Corolla", "Yaris" },
            eliminada, Fecha, null, eliminada ? Fecha : null);

    [Test]
    public async Task Create_MarcaSinId_GeneraObjectIdYPersisteDatosYAuditoria() {
        // Arrange
        var marca = CrearMarca(ObjectId.Empty);
        var antes = DateTime.UtcNow;

        // Act
        var resultado = await _repository.Create(marca);

        // Assert
        resultado.Id.Should().NotBe(ObjectId.Empty);
        resultado.Id.ToString().Should().MatchRegex("^[a-f0-9]{24}$");
        resultado.IsDeleted.Should().BeFalse();
        resultado.CreatedAt.Should().BeOnOrAfter(antes).And.BeOnOrBefore(DateTime.UtcNow);
        resultado.UpdatedAt.Should().BeNull();
        resultado.DeletedAt.Should().BeNull();
        var lectura = new MongoDbContext(_database);
        var guardada = await lectura.Marcas.Find(m => m.Id == resultado.Id).SingleAsync();
        guardada.Should().BeEquivalentTo(resultado,
            options => options.Using<DateTime>(c => c.Subject.Should().BeCloseTo(c.Expectation, TimeSpan.FromMilliseconds(1)))
                .WhenTypeIs<DateTime>());
    }

    [Test]
    public async Task Create_DosMarcasSinId_GeneraIdentificadoresDistintos() {
        // Arrange
        var primera = CrearMarca(ObjectId.Empty);
        var segunda = CrearMarca(ObjectId.Empty, "Honda");

        // Act
        var guardadaPrimera = await _repository.Create(primera);
        var guardadaSegunda = await _repository.Create(segunda);

        // Assert
        guardadaPrimera.Id.Should().NotBe(ObjectId.Empty);
        guardadaSegunda.Id.Should().NotBe(ObjectId.Empty).And.NotBe(guardadaPrimera.Id);
        var lectura = new MongoDbContext(_database);
        var guardadas = await lectura.Marcas.Find(_ => true).ToListAsync();
        guardadas.Select(m => m.Id).Should().BeEquivalentTo(new[] { guardadaPrimera.Id, guardadaSegunda.Id });
    }

    [Test]
    public async Task Create_MarcaConObjectIdAsignado_ConservaIdentificador() {
        // Arrange
        var marca = CrearMarca();

        // Act
        var resultado = await _repository.Create(marca);

        // Assert
        resultado.Id.Should().Be(Id);
        var lectura = new MongoDbContext(_database);
        var guardada = await lectura.Marcas.Find(m => m.Id == Id).SingleAsync();
        guardada.Nombre.Should().Be(marca.Nombre);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetById_MarcaExistente_DevuelveSoloSiEstaActiva(bool eliminada) {
        // Arrange
        var marca = CrearMarca(eliminada: eliminada);
        await _context.Marcas.InsertOneAsync(marca);

        // Act
        var resultado = await _repository.GetById(marca.Id.ToString());

        // Assert
        if (eliminada) resultado.Should().BeNull();
        else resultado.Should().BeEquivalentTo(marca);
    }

    [Test]
    public async Task GetAll_MarcasActivasYEliminadas_DevuelveActivasOrdenadas() {
        // Arrange
        await _context.Marcas.InsertManyAsync(new[] {
            CrearMarca(Id, "Toyota"), CrearMarca(OtroId, "BMW"), CrearMarca(TercerId, "Audi", true)
        });

        // Act
        var resultado = await _repository.GetAll();

        // Assert
        resultado.Select(m => m.Nombre).Should().Equal("BMW", "Toyota");
    }

    [Test]
    public async Task Update_MarcaActiva_ActualizaCatalogoSinCambiarIdentidadNiCreacion() {
        // Arrange
        var original = CrearMarca();
        await _context.Marcas.InsertOneAsync(original);
        var cambios = CrearMarca(OtroId, "Toyota actualizada", true) with {
            Pais = "Japón actualizado", Web = "https://example.com", ModelosDisponibles = new List<string> { "Prius" }
        };

        // Act
        var resultado = await _repository.Update(original.Id.ToString(), cambios);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Id.Should().Be(original.Id);
        resultado.CreatedAt.Should().Be(original.CreatedAt);
        resultado.IsDeleted.Should().BeFalse();
        resultado.DeletedAt.Should().BeNull();
        resultado.UpdatedAt.Should().NotBeNull();
        var lectura = new MongoDbContext(_database);
        var guardada = await lectura.Marcas.Find(m => m.Id == original.Id).SingleAsync();
        guardada.Nombre.Should().Be(cambios.Nombre);
        guardada.Pais.Should().Be(cambios.Pais);
        guardada.Web.Should().Be(cambios.Web);
        guardada.ModelosDisponibles.Should().Equal("Prius");
        guardada.CreatedAt.Should().Be(original.CreatedAt);
        guardada.IsDeleted.Should().BeFalse();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Update_MarcaInexistenteOEliminada_NoInsertaNiReactiva(bool existe) {
        // Arrange
        if (existe) await _context.Marcas.InsertOneAsync(CrearMarca(eliminada: true));

        // Act
        var resultado = await _repository.Update(Id.ToString(), CrearMarca(nombre: "Cambios"));

        // Assert
        resultado.Should().BeNull();
        (await _context.Marcas.CountDocumentsAsync(_ => true)).Should().Be(existe ? 1 : 0);
        if (existe) {
            var guardada = await _context.Marcas.Find(m => m.Id == Id).SingleAsync();
            guardada.Should().BeEquivalentTo(CrearMarca(eliminada: true));
        }
    }

    [Test]
    public async Task SoftDelete_MarcaActiva_ConservaDocumentoYDatosDelHistorico() {
        // Arrange
        var marca = CrearMarca();
        await _context.Marcas.InsertOneAsync(marca);

        // Act
        var resultado = await _repository.SoftDelete(marca.Id.ToString());

        // Assert
        resultado.Should().BeTrue();
        var lectura = new MongoDbContext(_database);
        var guardada = await lectura.Marcas.Find(m => m.Id == marca.Id).SingleAsync();
        guardada.IsDeleted.Should().BeTrue();
        guardada.DeletedAt.Should().NotBeNull();
        guardada.UpdatedAt.Should().Be(guardada.DeletedAt);
        guardada.Nombre.Should().Be(marca.Nombre);
        guardada.Pais.Should().Be(marca.Pais);
        guardada.Web.Should().Be(marca.Web);
        guardada.ModelosDisponibles.Should().Equal(marca.ModelosDisponibles);
        guardada.CreatedAt.Should().Be(marca.CreatedAt);
        (await _repository.GetById(marca.Id.ToString())).Should().BeNull();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SoftDelete_MarcaInexistenteOEliminada_DevuelveFalse(bool existe) {
        // Arrange
        if (existe) await _context.Marcas.InsertOneAsync(CrearMarca(eliminada: true));

        // Act
        var resultado = await _repository.SoftDelete(Id.ToString());

        // Assert
        resultado.Should().BeFalse();
        if (existe) {
            var guardada = await _context.Marcas.Find(m => m.Id == Id).SingleAsync();
            guardada.DeletedAt.Should().Be(Fecha);
        }
    }

    [TestCase(false, false, false)]
    [TestCase(true, false, true)]
    [TestCase(true, true, false)]
    public async Task ExistById_MarcaActivaEliminadaOInexistente_DevuelveResultadoEsperado(
        bool existe, bool eliminada, bool esperado) {
        // Arrange
        if (existe) await _context.Marcas.InsertOneAsync(CrearMarca(eliminada: eliminada));

        // Act
        var resultado = await _repository.ExistById(Id.ToString());

        // Assert
        resultado.Should().Be(esperado);
    }

    [Test]
    public async Task Create_MarcaConBajaYAuditoriaDeEntrada_NaceActivaConAuditoriaNueva() {
        // Arrange
        var marca = CrearMarca(eliminada: true) with { UpdatedAt = Fecha };
        var antes = DateTime.UtcNow;

        // Act
        var resultado = await _repository.Create(marca);

        // Assert
        resultado.IsDeleted.Should().BeFalse();
        resultado.CreatedAt.Should().BeOnOrAfter(antes).And.BeOnOrBefore(DateTime.UtcNow);
        resultado.UpdatedAt.Should().BeNull();
        resultado.DeletedAt.Should().BeNull();
        var lectura = new MongoDbContext(_database);
        var guardada = await lectura.Marcas.Find(m => m.Id == marca.Id).SingleAsync();
        guardada.IsDeleted.Should().BeFalse();
        guardada.UpdatedAt.Should().BeNull();
        guardada.DeletedAt.Should().BeNull();
        guardada.CreatedAt.Should().BeCloseTo(resultado.CreatedAt, TimeSpan.FromMilliseconds(1));
        guardada.ModelosDisponibles.Should().Equal(marca.ModelosDisponibles);
    }

    [Test]
    public async Task Create_IdDuplicado_RechazaElAltaYConservaLaMarcaOriginal() {
        // Arrange
        var original = CrearMarca();
        await _context.Marcas.InsertOneAsync(original);
        var repetida = CrearMarca(nombre: "No guardar");

        // Act
        Func<Task> act = () => _repository.Create(repetida);

        // Assert
        var error = await act.Should().ThrowAsync<MongoWriteException>();
        error.Which.WriteError.Category.Should().Be(ServerErrorCategory.DuplicateKey);
        var lectura = new MongoDbContext(_database);
        var guardadas = await lectura.Marcas.Find(_ => true).ToListAsync();
        guardadas.Should().ContainSingle().Which.Should().BeEquivalentTo(original);
    }

    [Test]
    public async Task Create_NombreRepetidoConIdsDistintos_ConservaAmbasMarcas() {
        // Arrange
        await _context.Marcas.InsertOneAsync(CrearMarca());
        var otra = CrearMarca(OtroId);

        // Act
        var resultado = await _repository.Create(otra);

        // Assert
        resultado.Id.Should().Be(OtroId);
        var lectura = new MongoDbContext(_database);
        var guardadas = await lectura.Marcas.Find(m => m.Nombre == "Toyota").ToListAsync();
        guardadas.Select(m => m.Id).Should().BeEquivalentTo(new[] { Id, OtroId });
    }

    [Test]
    public async Task GetById_IdentificadorValidoInexistente_DevuelveNullSinModificarOtrasMarcas() {
        // Arrange
        var original = CrearMarca();
        await _context.Marcas.InsertOneAsync(original);

        // Act
        var resultado = await _repository.GetById(OtroId.ToString());

        // Assert
        resultado.Should().BeNull();
        var lectura = new MongoDbContext(_database);
        var guardadas = await lectura.Marcas.Find(_ => true).ToListAsync();
        guardadas.Should().ContainSingle().Which.Should().BeEquivalentTo(original);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetAll_SinMarcasActivas_DevuelveColeccionVaciaYConservaHistorico(bool hayEliminada) {
        // Arrange
        var eliminada = CrearMarca(eliminada: true);
        if (hayEliminada) await _context.Marcas.InsertOneAsync(eliminada);

        // Act
        var resultado = await _repository.GetAll();

        // Assert
        resultado.Should().BeEmpty();
        var lectura = new MongoDbContext(_database);
        var guardadas = await lectura.Marcas.Find(_ => true).ToListAsync();
        guardadas.Should().HaveCount(hayEliminada ? 1 : 0);
        if (hayEliminada) guardadas.Single().Should().BeEquivalentTo(eliminada);
    }

    [Test]
    public async Task Update_DosMarcasExistentes_ModificaSoloLaIdentificadaPorLaClave() {
        // Arrange
        var original = CrearMarca();
        var otra = CrearMarca(OtroId, "Honda");
        await _context.Marcas.InsertManyAsync(new[] { original, otra });
        // El Id del cuerpo no debe cambiar cuál es el documento actualizado.
        var cambios = CrearMarca(OtroId, "Toyota renovada") with {
            ModelosDisponibles = new List<string> { "Prius", "RAV4" }
        };

        // Act
        var resultado = await _repository.Update(Id.ToString(), cambios);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Id.Should().Be(Id);
        var lectura = new MongoDbContext(_database);
        var guardada = await lectura.Marcas.Find(m => m.Id == Id).SingleAsync();
        guardada.Nombre.Should().Be(cambios.Nombre);
        guardada.ModelosDisponibles.Should().Equal("Prius", "RAV4");
        guardada.CreatedAt.Should().Be(original.CreatedAt);
        guardada.UpdatedAt.Should().NotBeNull();
        var intacta = await lectura.Marcas.Find(m => m.Id == OtroId).SingleAsync();
        intacta.Should().BeEquivalentTo(otra);
        (await lectura.Marcas.CountDocumentsAsync(_ => true)).Should().Be(2);
    }

    [Test]
    public async Task SoftDelete_DosMarcasActivas_DesactivaSoloLaIndicadaYLaOcultaDeLasConsultas() {
        // Arrange
        var marca = CrearMarca();
        var otra = CrearMarca(OtroId, "Honda");
        await _context.Marcas.InsertManyAsync(new[] { marca, otra });

        // Act
        var resultado = await _repository.SoftDelete(Id.ToString());

        // Assert
        resultado.Should().BeTrue();
        var lectura = new MongoDbContext(_database);
        (await lectura.Marcas.CountDocumentsAsync(_ => true)).Should().Be(2);
        var guardada = await lectura.Marcas.Find(m => m.Id == Id).SingleAsync();
        guardada.IsDeleted.Should().BeTrue();
        guardada.DeletedAt.Should().NotBeNull();
        guardada.UpdatedAt.Should().Be(guardada.DeletedAt);
        var intacta = await lectura.Marcas.Find(m => m.Id == OtroId).SingleAsync();
        intacta.Should().BeEquivalentTo(otra);
        (await _repository.GetById(Id.ToString())).Should().BeNull();
        (await _repository.ExistById(Id.ToString())).Should().BeFalse();
        (await _repository.GetAll()).Should().ContainSingle().Which.Id.Should().Be(OtroId);
    }
}
