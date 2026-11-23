using Alquiler_Vehiculos.Data.Seed.Vehiculo;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Data.Seed.Vehiculo;

[TestFixture]
[Category("Unit")]
public class VehiculoSeederTests {
    private static readonly DateTime Fecha = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);
    private VehiculoModel[] _vehiculos = null!;
    private MarcaModel[] _marcas = null!;
    private ConcesionarioModel[] _concesionarios = null!;
    private Foto[] _fotos = null!;

    [SetUp]
    public void SetUp() {
        _vehiculos = VehiculoSeeder.BuildVehiculos(Fecha);
        _marcas = MarcaSeeder.BuildMarcas(Fecha);
        // Simular IDs ya generados por PostgreSQL, sin depender de una secuencia que empiece por 1.
        _concesionarios = ConcesionarioSeeder.BuildConcesionarios(Fecha)
            .Select((c, i) => c with { Id = (i + 1) * 100 }).ToArray();
        _fotos = _vehiculos.Select(v => new Foto {
            Nombre = $"seed-{v.Matrícula}.jpg", Ruta = $"/uploads/vehiculos/seed-{v.Matrícula}.jpg",
            Extension = ".jpg", Tamano = 2048, Activo = true, CreatedAt = Fecha
        }).ToArray();
    }

    [Test]
    public void BuildVehiculos_FlotaInicial_DevuelveDoceMatriculasValidasYUnicas() {
        // Arrange
        var fecha = Fecha;

        // Act
        var vehiculos = VehiculoSeeder.BuildVehiculos(fecha);

        // Assert
        vehiculos.Should().HaveCount(12);
        vehiculos.Select(v => v.Matrícula).Should().OnlyHaveUniqueItems();
        foreach (var vehiculo in vehiculos) {
            vehiculo.Matrícula.Should().MatchRegex("^[0-9]{4}[BCDFGHJKLMNPRSTVWXYZ]{3}$");
        }
        vehiculos.Should().OnlyContain(v => v.Id == null && v.ConesionarioFk == 0 && v.FotoFk == string.Empty);
    }

    [Test]
    public void BuildVehiculos_FlotaInicial_CubreTodasLasClasificacionesYEstados() {
        // Arrange
        var fecha = Fecha;

        // Act
        var vehiculos = VehiculoSeeder.BuildVehiculos(fecha);

        // Assert
        vehiculos.Select(v => v.Carroceria).Distinct().Should().BeEquivalentTo(Enum.GetValues<TipoCarroceria>());
        vehiculos.Select(v => v.Combustible).Distinct().Should().BeEquivalentTo(Enum.GetValues<TipoCombustible>());
        vehiculos.Select(v => v.Transmision).Distinct().Should().BeEquivalentTo(Enum.GetValues<TipoTransmision>());
        vehiculos.Select(v => v.Estado).Distinct().Should().BeEquivalentTo(Enum.GetValues<EstadoVehiculo>());
        vehiculos.Select(v => v.Modelo).Distinct().Should().HaveCount(9);
    }

    [TestCase("Corolla Sedán", "ILAH", TipoCarroceria.Sedan, TipoCombustible.Hibrido, TipoTransmision.Automatica)]
    [TestCase("Megane", "CDMD", TipoCarroceria.Hatchback, TipoCombustible.Diesel, TipoTransmision.Manual)]
    [TestCase("Ariya", "IFAE", TipoCarroceria.Suv, TipoCombustible.Electrico, TipoTransmision.Automatica)]
    [TestCase("Wrangler", "FJBD", TipoCarroceria.TodoTerreno, TipoCombustible.Diesel, TipoTransmision.Automatica)]
    [TestCase("Passat Variant", "IWMD", TipoCarroceria.Familiar, TipoCombustible.Diesel, TipoTransmision.Manual)]
    [TestCase("Serie 4 Coupé", "PEAV", TipoCarroceria.Coupe, TipoCombustible.Gasolina, TipoTransmision.Automatica)]
    [TestCase("Cabrio", "MTAV", TipoCarroceria.Descapotable, TipoCombustible.Gasolina, TipoTransmision.Automatica)]
    [TestCase("Grand Scénic", "IMMD", TipoCarroceria.Monovolumen, TipoCombustible.Diesel, TipoTransmision.Manual)]
    [TestCase("Transit", "FKMD", TipoCarroceria.Furgoneta, TipoCombustible.Diesel, TipoTransmision.Manual)]
    public void BuildVehiculos_ModelosDelEnunciado_ConservaAcrissYCaracteristicas(string modelo, string acriss,
        TipoCarroceria carroceria, TipoCombustible combustible, TipoTransmision transmision) {
        // Arrange
        var fecha = Fecha;

        // Act
        var vehiculos = VehiculoSeeder.BuildVehiculos(fecha).Where(v => v.Modelo == modelo).ToArray();

        // Assert
        vehiculos.Should().NotBeEmpty().And.OnlyContain(v => v.Acriss == acriss && v.Carroceria == carroceria &&
            v.Combustible == combustible && v.Transmision == transmision);
    }

    [Test]
    public void BuildVehiculos_FechaFija_DevuelveTarifasYCaracteristicasPositivasYFechasUtc() {
        // Arrange
        var fecha = Fecha;

        // Act
        var vehiculos = VehiculoSeeder.BuildVehiculos(fecha);

        // Assert
        vehiculos.Should().OnlyContain(v => v.PrecioBaseDia > 0 && v.Plazas > 0 && v.MaleteroLitros > 0 &&
            v.AutonomiaKm > 0 && v.Kilometraje >= 0);
        vehiculos.Should().OnlyContain(v => v.FechaUltimaRevision < fecha && v.FechaUltimaRevision.Kind == DateTimeKind.Utc &&
            v.CreatedAt <= fecha && v.CreatedAt.Kind == DateTimeKind.Utc);
    }

    [Test]
    public void BuildVehiculos_MiniRetirado_ConservaBajaLogicaYAuditoriaCoherentes() {
        // Arrange
        var fecha = Fecha;

        // Act
        var vehiculos = VehiculoSeeder.BuildVehiculos(fecha);

        // Assert
        var retirado = vehiculos.Should().ContainSingle(v => v.Estado == EstadoVehiculo.Retirado).Which;
        retirado.Matrícula.Should().Be("7890JKL");
        retirado.Activo.Should().BeFalse();
        retirado.CreatedAt.Should().Be(fecha.AddYears(-2));
        retirado.UpdatedAt.Should().Be(fecha);
        retirado.DeletedAt.Should().Be(fecha);
        vehiculos.Where(v => v.Estado != EstadoVehiculo.Retirado).Should()
            .OnlyContain(v => v.Activo && v.UpdatedAt == null && v.DeletedAt == null);
    }

    [TestCase("1234BCD", EstadoVehiculo.Disponible)]
    [TestCase("2345CDF", EstadoVehiculo.Reservado)]
    [TestCase("0123NPR", EstadoVehiculo.EnAlquiler)]
    [TestCase("1357PRS", EstadoVehiculo.Reservado)]
    [TestCase("9012MNP", EstadoVehiculo.Mantenimiento)]
    [TestCase("2468RST", EstadoVehiculo.Disponible)]
    public void BuildVehiculos_EscenariosParaLasSemillasDelGrupo_ConservaEstadoPorMatricula(string matricula, EstadoVehiculo estado) {
        // Arrange
        var fecha = Fecha;

        // Act
        var vehiculo = VehiculoSeeder.BuildVehiculos(fecha).Single(v => v.Matrícula == matricula);

        // Assert
        vehiculo.Estado.Should().Be(estado);
        vehiculo.Activo.Should().BeTrue();
    }

    [TestCase("Megane")]
    [TestCase("Corolla Sedán")]
    [TestCase("Transit")]
    public void BuildVehiculos_ModelosRepetidos_DevuelveDosUnidadesConDistintaTarifa(string modelo) {
        // Arrange
        var fecha = Fecha;

        // Act
        var unidades = VehiculoSeeder.BuildVehiculos(fecha).Where(v => v.Modelo == modelo).ToArray();

        // Assert
        unidades.Should().HaveCount(2);
        unidades.Select(v => v.Matrícula).Should().OnlyHaveUniqueItems();
        unidades.Select(v => v.PrecioBaseDia).Should().OnlyHaveUniqueItems();
        unidades.Select(v => v.Acriss).Distinct().Should().ContainSingle();
    }

    [Test]
    public void PrepararVehiculos_CatalogosDesordenados_AsociaIdsRealesYFotosSinCambiarLosOriginales() {
        // Arrange
        var vehiculos = _vehiculos.ToArray();
        string[] concesionariosEsperados = [
            "Motor Leganés", "Motor Leganés", "Automóviles Madrid Centro", "Motor Getafe",
            "Vehículos Alcorcón", "Automóviles Madrid Centro", "Motor Fuenlabrada",
            "Motor Leganés", "Motor Getafe", "Vehículos Alcorcón", "Motor Getafe", "Vehículos Alcorcón"
        ];

        // Act
        var preparados = VehiculoSeeder.PrepararVehiculos(vehiculos, _marcas.Reverse().ToArray(),
            _concesionarios.Reverse().ToArray(), _fotos.Reverse().ToArray(), "uploads");

        // Assert
        preparados.Should().HaveCount(12);
        for (var i = 0; i < preparados.Length; i++) {
            var preparado = preparados[i];
            preparado.ConesionarioFk.Should().Be(_concesionarios.Single(c => c.Nombre == concesionariosEsperados[i]).Id);
            preparado.Foto.Should().BeSameAs(_fotos.Single(f => f.Nombre == $"seed-{preparado.Matrícula}.jpg"));
            preparado.FotoFk.Should().Be(preparado.Foto.Nombre);
            preparado.Marca.Should().Be(vehiculos[i].Marca);
            preparado.Estado.Should().Be(vehiculos[i].Estado);
        }
        vehiculos.Should().OnlyContain(v => v.ConesionarioFk == 0 && v.FotoFk == string.Empty && v.Foto == null);
        preparados.Select(v => v.FotoFk).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void PrepararVehiculos_CarpetaPersonalizada_ConservaLaRutaYAsociaElNombreDeLaFoto() {
        // Arrange
        var fotos = _fotos.Select(f => f with { Ruta = f.Ruta.Replace("/uploads/", "/imagenes/") }).ToArray();

        // Act
        var preparados = VehiculoSeeder.PrepararVehiculos(_vehiculos, _marcas, _concesionarios, fotos, "imagenes");

        // Assert
        preparados.Should().OnlyContain(v => v.FotoFk == v.Foto.Nombre &&
            v.Foto.Ruta.StartsWith("/imagenes/vehiculos/"));
    }

    [TestCase("Toyota")]
    [TestCase("MINI")]
    public void PrepararVehiculos_MarcaAusente_RechazaInclusoSiElVehiculoEstaRetirado(string nombre) {
        // Arrange
        var marcas = _marcas.Where(m => m.Nombre != nombre).ToArray();

        // Act
        Action act = () => VehiculoSeeder.PrepararVehiculos(_vehiculos, marcas, _concesionarios, _fotos, "uploads");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage($"*{nombre}*Ejecuta primero las semillas*");
        _vehiculos.Should().OnlyContain(v => v.ConesionarioFk == 0 && v.FotoFk == string.Empty);
    }

    [Test]
    public void PrepararVehiculos_ModeloNoCatalogado_RechazaLaAsociacion() {
        // Arrange
        var marcas = _marcas.Select(m => m.Nombre == "Toyota" ? m with { ModelosDisponibles = new List<string>() } : m).ToArray();

        // Act
        Action act = () => VehiculoSeeder.PrepararVehiculos(_vehiculos, marcas, _concesionarios, _fotos, "uploads");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*Toyota Corolla Sedán*");
    }

    [Test]
    public void PrepararVehiculos_MarcaDesactivadaConVehiculoActivo_RechazaLaAsociacion() {
        // Arrange
        var marcas = _marcas.Select(m => m.Nombre == "Toyota" ? m with { IsDeleted = true } : m).ToArray();

        // Act
        Action act = () => VehiculoSeeder.PrepararVehiculos(_vehiculos, marcas, _concesionarios, _fotos, "uploads");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*Toyota Corolla Sedán*");
    }

    [TestCase("Motor Leganés")]
    [TestCase("Motor Fuenlabrada")]
    public void PrepararVehiculos_ConcesionarioAusente_RechazaLaAsociacion(string nombre) {
        // Arrange
        var concesionarios = _concesionarios.Where(c => c.Nombre != nombre).ToArray();

        // Act
        Action act = () => VehiculoSeeder.PrepararVehiculos(_vehiculos, _marcas, concesionarios, _fotos, "uploads");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*Faltan datos de catálogo coherentes*");
    }

    [Test]
    public void PrepararVehiculos_ConcesionarioDesactivadoConVehiculoActivo_RechazaLaAsociacion() {
        // Arrange
        var concesionarios = _concesionarios.Select(c => c.Nombre == "Motor Leganés" ? c with { IsDeleted = true } : c).ToArray();

        // Act
        Action act = () => VehiculoSeeder.PrepararVehiculos(_vehiculos, _marcas, concesionarios, _fotos, "uploads");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*Toyota Corolla Sedán*");
    }

    [Test]
    public void PrepararVehiculos_MiniRetirado_AdmiteSuMarcaYConcesionarioDesactivados() {
        // Arrange
        var marca = _marcas.Single(m => m.Nombre == "MINI");
        var concesionario = _concesionarios.Single(c => c.Nombre == "Motor Fuenlabrada");

        // Act
        var mini = VehiculoSeeder.PrepararVehiculos(_vehiculos, _marcas, _concesionarios, _fotos, "uploads")
            .Single(v => v.Marca == "MINI");

        // Assert
        marca.IsDeleted.Should().BeTrue();
        concesionario.IsDeleted.Should().BeTrue();
        mini.Activo.Should().BeFalse();
        mini.Estado.Should().Be(EstadoVehiculo.Retirado);
        mini.ConesionarioFk.Should().Be(concesionario.Id);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PrepararVehiculos_FotoAusenteOInactiva_RechazaLaAsociacion(bool inactiva) {
        // Arrange
        var fotos = inactiva
            ? _fotos.Select(f => f.Nombre == "seed-1234BCD.jpg" ? f with { Activo = false } : f).ToArray()
            : _fotos.Where(f => f.Nombre != "seed-1234BCD.jpg").ToArray();

        // Act
        Action act = () => VehiculoSeeder.PrepararVehiculos(_vehiculos, _marcas, _concesionarios, fotos, "uploads");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*Falta la foto activa*1234BCD*");
    }
}
