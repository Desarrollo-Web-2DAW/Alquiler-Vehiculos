using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Mappers.Vehiculo;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Models.Vehiculo.Enums.ACRISS;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Mappers;

/// <summary>
/// Tests unitarios de <see cref="VehiculoMapper"/>.
/// El foco principal es el parseo del código ACRISS a los enums de dominio
/// (<see cref="TipoCarroceria"/>, <see cref="TipoCombustible"/> y <see cref="TipoTransmision"/>),
/// cubriendo cada letra del catálogo, las letras desconocidas y los tres errores de dominio.
/// </summary>
/// <remarks>
/// Convención ACRISS estándar: <c>[0]</c> categoría (no se mapea), <c>[1]</c> tipo
/// de vehículo, <c>[2]</c> transmisión y tracción, <c>[3]</c> combustible y climatización.
/// </remarks>
[TestFixture]
[Category("Unit")]
public class VehiculoMapperTests
{
    // C = Compacto, L = Berlina, D = Automática AWD, H = Híbrido.
    private const string AcrissValido = "CLDH";

    private static CreateVehiculoDto CrearDto(string acriss = AcrissValido) => new()
    {
        Foto = "delantera.jpg",
        Matricula = "1234BCD",
        Marca = "Toyota",
        Modelo = "Corolla",
        Acriss = acriss,
        Color = "Azul",
        PrecioBaseDia = 45.90m,
        Plazas = 5,
        MaleteroLitros = 361,
        AutonomiaKm = 780,
        Kilometraje = 12_500,
        FechaUltimaRevision = "2026-10-01",
        Concesionario = 10L
    };

    private static UpdateVehiculoDto CrearUpdateDto(string acriss = AcrissValido) => new()
    {
        Foto = "delantera.jpg",
        Matricula = "1234BCD",
        Marca = "Toyota",
        Modelo = "Corolla",
        Acriss = acriss,
        Estado = "Disponible",
        Color = "Azul",
        PrecioBaseDia = 45.90m,
        Plazas = 5,
        MaleteroLitros = 361,
        AutonomiaKm = 780,
        Kilometraje = 12_500,
        FechaUltimaRevision = "2026-10-01",
        Concesionario = 10L,
        IsActivo = true
    };

    private static Foto CrearFoto() => new()
    {
        Nombre = "delantera.jpg",
        Ruta = "/uploads/vehiculos/delantera.jpg",
        Extension = ".jpg",
        Tamano = 1024
    };

    private static VehiculoModel CrearModelo()
    {
        var vehiculo = new VehiculoModel
        {
            FotoFk = "delantera.jpg",
            Matrícula = "1234BCD",
            Marca = "Toyota",
            Modelo = "Corolla",
            Acriss = AcrissValido,
            Carroceria = TipoCarroceria.Sedan,
            Combustible = TipoCombustible.Hibrido,
            Transmision = TipoTransmision.Automatica,
            Color = TipoColor.Rojo,
            PrecioBaseDia = 45.90m,
            Plazas = 5,
            MaleteroLitros = 361,
            AutonomiaKm = 780,
            Kilometraje = 12_500,
            FechaUltimaRevision = new DateTime(2026, 10, 1),
            ConesionarioFk = 10L,
            Activo = true
        };
        vehiculo.CambiarEstadoMapper(EstadoVehiculo.EnAlquiler);
        return vehiculo;
    }

    /// <summary>
    /// Construye un ACRISS de cuatro letras colocando la letra bajo test en la posición
    /// que corresponde y dejando el resto en valores válidos, para que el fallo esperado
    /// sea siempre el del paso que se está probando.
    /// </summary>
    private static string CrearAcriss(char tipo, char transmision = 'A', char combustible = 'H') =>
        $"C{tipo}{transmision}{combustible}";

    [Test]
    public void ToVehiculo_ConAcrissValido_DevuelveExito()
    {
        // Arrange
        var dto = CrearDto();

        // Act
        var resultado = dto.ToVehiculo(CrearFoto());

        // Assert
        resultado.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ToVehiculo_ConDtoValido_CopiaLosDatosDelVehiculo()
    {
        // Arrange
        var dto = CrearDto();
        var foto = CrearFoto();

        // Act
        var resultado = dto.ToVehiculo(foto);

        // Assert
        resultado.Value.Should().BeEquivalentTo(new
        {
            FotoFk = "delantera.jpg",
            Foto = foto,
            Matrícula = "1234BCD",
            Marca = "Toyota",
            Modelo = "Corolla",
            Acriss = AcrissValido,
            PrecioBaseDia = 45.90m,
            Plazas = 5,
            MaleteroLitros = 361,
            AutonomiaKm = 780,
            Kilometraje = 12_500,
            FechaUltimaRevision = new DateTime(2026, 10, 1),
            ConesionarioFk = 10L,
            Activo = true
        });
    }

    [Test]
    public void ToVehiculo_ConFoto_UsaElNombreComoClaveForaneaYConservaLaReferencia()
    {
        // Arrange
        var foto = CrearFoto();

        // Act
        var resultado = CrearDto().ToVehiculo(foto);

        // Assert
        resultado.Value.FotoFk.Should().Be(foto.Nombre);
        resultado.Value.Foto.Should().BeSameAs(foto);
    }

    [Test]
    public void ToVehiculo_CuandoSeCrea_NaceDisponibleYActivo()
    {
        // Arrange
        var dto = CrearDto();

        // Act
        var resultado = dto.ToVehiculo(CrearFoto());

        // Assert — RF-01: un vehículo nuevo nace siempre Disponible.
        (resultado.Value.Estado, resultado.Value.Activo).Should().Be((EstadoVehiculo.Disponible, true));
    }

    [TestCase("Disponible", EstadoVehiculo.Disponible)]
    [TestCase("Reservado", EstadoVehiculo.Reservado)]
    [TestCase("EnAlquiler", EstadoVehiculo.EnAlquiler)]
    [TestCase("Mantenimiento", EstadoVehiculo.Mantenimiento)]
    [TestCase("enAlquiler", EstadoVehiculo.EnAlquiler)]
    public void ToVehiculoModel_ConEstadoDelCatalogo_MapeaElEstado(string estado, EstadoVehiculo esperado)
    {
        // Arrange
        var dto = CrearUpdateDto();
        dto.Estado = estado;

        // Act
        var resultado = dto.ToVehiculoModel(CrearFoto());

        // Assert
        resultado.Value.Estado.Should().Be(esperado);
    }

    [TestCase("Azul", TipoColor.Azul)]
    [TestCase("azul", TipoColor.Azul)]
    [TestCase("ROJO", TipoColor.Rojo)]
    [TestCase("fucsia", TipoColor.Fucsia)]
    public void ToVehiculo_ConColorEnCualquierCombinacionDeMayusculas_MapeaElTipoDeColor(
        string color, TipoColor esperado)
    {
        // Arrange
        var dto = CrearDto();
        dto.Color = color;

        // Act
        var resultado = dto.ToVehiculo(CrearFoto());

        // Assert
        resultado.Value.Color.Should().Be(esperado);
    }

    [Test]
    public void ToVehiculo_ConFechaValida_ConvierteElTextoAFecha()
    {
        // Arrange
        var dto = CrearDto();
        dto.FechaUltimaRevision = "2026-10-01";

        // Act
        var resultado = dto.ToVehiculo(CrearFoto());

        // Assert
        resultado.Value.FechaUltimaRevision.Should().Be(new DateTime(2026, 10, 1));
    }

    [Test]
    public void ToVehiculo_ConFechaNoParseable_UsaLaFechaActual()
    {
        // Arrange
        var antes = DateTime.UtcNow;
        var dto = CrearDto();
        dto.FechaUltimaRevision = "no-es-una-fecha";

        // Act
        var resultado = dto.ToVehiculo(CrearFoto());

        // Assert
        resultado.Value.FechaUltimaRevision.Should().BeCloseTo(antes, TimeSpan.FromMinutes(1));
    }

    [Test]
    public void ToVehiculo_CuandoSeCrea_RegistraLaFechaActualDeCreacion()
    {
        // Arrange
        var antes = DateTime.UtcNow;

        // Act
        var resultado = CrearDto().ToVehiculo(CrearFoto());

        // Assert
        resultado.Value.CreatedAt.Should().BeCloseTo(antes, TimeSpan.FromMinutes(1));
    }

    [Test]
    public void ToVehiculo_CuandoSeCrea_NoTieneFechaDeModificacionNiDeBorrado()
    {
        // Arrange
        var dto = CrearDto();

        // Act
        var resultado = dto.ToVehiculo(CrearFoto());

        // Assert
        (resultado.Value.UpdatedAt, resultado.Value.DeletedAt).Should().Be((null, null));
    }

    [Test]
    public void ToVehiculo_ConAcrissDemasiadoCorto_LanzaIndexOutOfRange()
    {
        // Arrange
        var dto = CrearDto("C");

        // Act
        Action mapear = () => dto.ToVehiculo(CrearFoto());

        // Assert
        mapear.Should().Throw<IndexOutOfRangeException>();
    }

    [TestCase('B', TipoCarroceria.Hatchback)]
    [TestCase('C', TipoCarroceria.Hatchback)]
    [TestCase('D', TipoCarroceria.Hatchback)]
    [TestCase('W', TipoCarroceria.Familiar)]
    [TestCase('V', TipoCarroceria.Furgoneta)]
    [TestCase('K', TipoCarroceria.Furgoneta)]
    [TestCase('F', TipoCarroceria.Suv)]
    [TestCase('J', TipoCarroceria.TodoTerreno)]
    [TestCase('T', TipoCarroceria.Descapotable)]
    [TestCase('E', TipoCarroceria.Coupe)]
    [TestCase('M', TipoCarroceria.Monovolumen)]
    [TestCase('L', TipoCarroceria.Sedan)]
    public void ToVehiculo_ConSegundaLetraDelCatalogo_MapeaLaCarroceria(
        char letra, TipoCarroceria esperada)
    {
        // Arrange
        var dto = CrearDto(CrearAcriss(letra));

        // Act
        var resultado = dto.ToVehiculo(CrearFoto());

        // Assert
        resultado.Value.Carroceria.Should().Be(esperada);
    }

    /// <summary>
    /// Letras cuyo <see cref="AcrissTipo"/> no tiene equivalente en <see cref="TipoCarroceria"/>
    /// (Deportivo, Roadster, Crossover, pickups, autocaravana, recreativo, especial y oferta especial).
    /// </summary>
    private static readonly char[] LetrasSinCarroceria = ['G', 'H', 'N', 'P', 'Q', 'R', 'S', 'X', 'Z'];

    [TestCase('A')]
    [TestCase('G')]
    [TestCase('H')]
    [TestCase('I')]
    [TestCase('N')]
    [TestCase('O')]
    [TestCase('P')]
    [TestCase('Q')]
    [TestCase('R')]
    [TestCase('S')]
    [TestCase('U')]
    [TestCase('X')]
    [TestCase('Y')]
    [TestCase('Z')]
    [TestCase('c')]
    public void ToVehiculo_ConSegundaLetraNoMapeable_DevuelveErrorDeCarroceria(char letra)
    {
        // Arrange
        var dto = CrearDto(CrearAcriss(letra));

        // Act
        var resultado = dto.ToVehiculo(CrearFoto());

        // Assert
        resultado.Error.Should().Be(VehiculoError.TipoDeCarroceria());
    }

    [Test]
    public void ToVehiculo_ConCualquierAcrissTipoDelCatalogo_SoloFallaSiEstaFueraDelMapaDeCarrocerias()
    {
        // Arrange
        var tipos = Enum.GetValues<AcrissTipo>();

        // Act
        var sinCarroceria = tipos
            .Select(tipo => (Tipo: (char)tipo, Resultado: CrearDto(CrearAcriss((char)tipo)).ToVehiculo(CrearFoto())))
            .Where(x => x.Resultado.IsFailure && x.Resultado.Error == VehiculoError.TipoDeCarroceria())
            .Select(x => x.Tipo)
            .ToArray();

        // Assert
        sinCarroceria.Should().BeEquivalentTo(LetrasSinCarroceria,
            "todo AcrissTipo debe tener carrocería salvo los que están en el catálogo sin equivalente");
    }

    [TestCase('M')]
    [TestCase('E')]
    [TestCase('S')]
    [TestCase('X')]
    public void ToVehiculo_AlCambiarLaPrimeraLetra_MantieneLaCarroceria(char categoria)
    {
        // Arrange
        var dto = CrearDto($"{categoria}LDH");

        // Act
        var resultado = dto.ToVehiculo(CrearFoto());

        // Assert
        resultado.Value.Carroceria.Should().Be(TipoCarroceria.Sedan);
    }

    [TestCase('M', TipoTransmision.Manual)]          // ManualSinTraccionDefinida
    [TestCase('N', TipoTransmision.Manual)]          // Manual4Wd
    [TestCase('C', TipoTransmision.Manual)]          // ManualAwd
    [TestCase('A', TipoTransmision.Automatica)]      // AutomaticaSinTraccionDefinida
    [TestCase('B', TipoTransmision.Automatica)]      // Automatica4Wd
    [TestCase('D', TipoTransmision.Automatica)]      // AutomaticaAwd
    public void ToVehiculo_ConTerceraLetraDelCatalogo_MapeaLaTransmision(
        char letra, TipoTransmision esperada)
    {
        // Arrange
        var dto = CrearDto(CrearAcriss('L', letra));

        // Act
        var resultado = dto.ToVehiculo(CrearFoto());

        // Assert
        resultado.Value.Transmision.Should().Be(esperada);
    }

    [TestCase('F')] // AutomatizacionNivel5
    [TestCase('H')] // AutomatizacionNivel4
    [TestCase('Q')] // AutomatizacionNivel3
    [TestCase('V')] // letra fuera del catálogo
    public void ToVehiculo_ConTerceraLetraSinEquivalente_DevuelveErrorDeTransmision(char letra)
    {
        // Arrange
        var dto = CrearDto(CrearAcriss('L', letra));

        // Act
        var resultado = dto.ToVehiculo(CrearFoto());

        // Assert
        resultado.Error.Should().Be(VehiculoError.TipoDeTransmision());
    }

    [Test]
    public void ToVehiculo_ConCualquierTransmisionDelCatalogo_SoloFallaSiEstaFueraDelMapa()
    {
        // Arrange
        var transmisiones = Enum.GetValues<AcrissTransmisionTraccion>();
        char[] sinEquivalente = ['F', 'H', 'Q'];

        // Act
        var fallidas = transmisiones
            .Select(transmision => (
                Letra: (char)transmision,
                Resultado: CrearDto(CrearAcriss('L', (char)transmision)).ToVehiculo(CrearFoto())))
            .Where(x => x.Resultado.IsFailure && x.Resultado.Error == VehiculoError.TipoDeTransmision())
            .Select(x => x.Letra)
            .ToArray();

        // Assert
        fallidas.Should().BeEquivalentTo(sinEquivalente,
            "solo las automatizaciones sin equivalente deben devolver error de transmisión");
    }

    [TestCase('D', TipoCombustible.Diesel)]    // DieselConAire
    [TestCase('Q', TipoCombustible.Diesel)]    // DieselSinAire
    [TestCase('V', TipoCombustible.Gasolina)]  // GasolinaConAire
    [TestCase('Z', TipoCombustible.Gasolina)]  // GasolinaSinAire
    [TestCase('H', TipoCombustible.Hibrido)]   // Hibrido
    [TestCase('I', TipoCombustible.Hibrido)]   // HibridoEnchufable
    [TestCase('E', TipoCombustible.Electrico)] // ElectricoE
    [TestCase('C', TipoCombustible.Electrico)] // ElectricoC
    public void ToVehiculo_ConCuartaLetraDelCatalogo_MapeaElCombustible(
        char letra, TipoCombustible esperado)
    {
        // Arrange
        var dto = CrearDto(CrearAcriss('L', 'A', letra));

        // Act
        var resultado = dto.ToVehiculo(CrearFoto());

        // Assert
        resultado.Value.Combustible.Should().Be(esperado);
    }

    [TestCase('A')] // HidrogenoConAire
    [TestCase('B')] // HidrogenoSinAire
    [TestCase('F')] // MulticombustibleSinAire
    [TestCase('U')] // EtanolConAire
    [TestCase('L')] // GlpConAire
    [TestCase('M')] // MulticomustibleConAire
    [TestCase('N')] // SinEspecificarSinAire
    [TestCase('R')] // SinEspecificarConAire
    [TestCase('S')] // GlpSinAire
    [TestCase('X')] // EtanolSinAire
    [TestCase('K')] // letra fuera del catálogo
    public void ToVehiculo_ConCuartaLetraSinEquivalente_DevuelveErrorDeCombustible(char letra)
    {
        // Arrange
        var dto = CrearDto(CrearAcriss('L', 'A', letra));

        // Act
        var resultado = dto.ToVehiculo(CrearFoto());

        // Assert
        resultado.Error.Should().Be(VehiculoError.TipoCombustible());
    }

    [Test]
    public void ToVehiculoModel_ConDtoValido_MapeaMarcaModeloConcesionarioYFoto()
    {
        // Arrange
        var dto = CrearUpdateDto();
        var foto = CrearFoto();

        // Act
        var resultado = dto.ToVehiculoModel(foto);

        // Assert
        resultado.Value.Should().BeEquivalentTo(new
        {
            FotoFk = "delantera.jpg",
            Foto = foto,
            Matrícula = "1234BCD",
            Marca = "Toyota",
            Modelo = "Corolla",
            Acriss = AcrissValido,
            Carroceria = TipoCarroceria.Sedan,
            Combustible = TipoCombustible.Hibrido,
            Transmision = TipoTransmision.Automatica,
            ConesionarioFk = 10L,
            Estado = EstadoVehiculo.Disponible,
            Activo = true
        });
    }

    [Test]
    public void ToVehiculoModel_ConSegundaLetraNoMapeable_DevuelveErrorDeCarroceria()
    {
        // Arrange
        var dto = CrearUpdateDto(CrearAcriss('S'));

        // Act
        var resultado = dto.ToVehiculoModel(CrearFoto());

        // Assert
        resultado.Error.Should().Be(VehiculoError.TipoDeCarroceria());
    }

    [Test]
    public void ToDto_ConModelo_DevuelveUnDtoConLosMismosDatos()
    {
        // Arrange
        var modelo = CrearModelo();

        // Act
        var dto = modelo.ToDto();

        // Assert
        dto.Should().BeEquivalentTo(new
        {
            Foto = "delantera.jpg",
            Matricula = "1234BCD",
            Marca = "Toyota",
            Modelo = "Corolla",
            Acriss = AcrissValido,
            Categoria = "Sedan",
            Combustible = "Hibrido",
            Transmision = "Automatica",
            Estado = "EnAlquiler",
            Color = "Rojo",
            PrecioBaseDia = 45.90m,
            Plazas = 5,
            MaleteroLitros = 361,
            AutonomiaKm = 780,
            Kilometraje = 12_500,
            FechaUltimaRevision = "2026-10-01",
            Concesionario = 10L,
            IsActivo = true
        });
    }

    [Test]
    public void ToDto_ConFechaDeUltimaRevision_LaFormateaComoAnioMesDia()
    {
        // Arrange
        var modelo = CrearModelo() with { FechaUltimaRevision = new DateTime(2025, 3, 9) };

        // Act
        var dto = modelo.ToDto();

        // Assert
        dto.FechaUltimaRevision.Should().Be("2025-03-09");
    }

    [Test]
    public void ToDto_ConVehiculoInactivo_DevuelveIsActivoFalse()
    {
        // Arrange
        var modelo = CrearModelo() with { Activo = false };

        // Act
        var dto = modelo.ToDto();

        // Assert
        dto.IsActivo.Should().BeFalse();
    }
}
