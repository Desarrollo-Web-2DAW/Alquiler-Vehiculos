using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Mappers.Vehiculo;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Models.Vehiculo.Enums.ACRISS;
using FluentAssertions;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Vehiculo;

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
    [TestCase("5")]
    [TestCase("999")]
    [TestCase("-1")]
    [TestCase("Desconocido")]
    public void Color_TextoNumericoOValorNoDefinido_SeRechazaSinLanzarExcepcion(string texto)
    {
        // Arrange
        var create = CrearDto() with { Color = texto };
        var update = CrearUpdateDto() with { Color = texto };

        // Act
        var alta = create.ToVehiculo(CrearFoto());
        var modificacion = update.ToVehiculoModel(CrearFoto());
        var validacionAlta = new CreateVehiculoValidator().Validate(create);
        var validacionModificacion = new UpdateVehiculoValidator().Validate(update);

        // Assert
        alta.IsFailure.Should().BeTrue();
        alta.Error.Should().BeEquivalentTo(VehiculoError.TipoColorInvalido());
        modificacion.IsFailure.Should().BeTrue();
        modificacion.Error.Should().BeEquivalentTo(VehiculoError.TipoColorInvalido());
        validacionAlta.Errors.Should().ContainSingle(e => e.PropertyName == "Color");
        validacionModificacion.Errors.Should().ContainSingle(e => e.PropertyName == "Color");
    }

    [TestCase(1, "BCDEFGHJKLMNPQRSTVW")]
    [TestCase(2, "MNCABDQHF")]
    [TestCase(3, "ABCDEFHILMNQRSUVXZ")]
    public void Acriss_TodasLasLetras_ValidadoresYMapeadoresCoinciden(int posicion, string admitidas)
    {
        // Arrange
        var createValidator = new CreateVehiculoValidator();
        var updateValidator = new UpdateVehiculoValidator();
        var letras = Enumerable.Range('A', 26).Select(c => (char)c).ToArray();

        // Act
        var resultados = letras.Select(letra => {
            var codigo = "CLAH".ToCharArray();
            codigo[posicion] = letra;
            var create = CrearDto(new string(codigo));
            var update = CrearUpdateDto(new string(codigo));
            return new {
                Letra = letra,
                CreateValido = createValidator.Validate(create).IsValid,
                UpdateValido = updateValidator.Validate(update).IsValid,
                CreateMapeable = create.ToVehiculo(CrearFoto()).IsSuccess,
                UpdateMapeable = update.ToVehiculoModel(CrearFoto()).IsSuccess
            };
        }).ToArray();

        // Assert
        foreach (var resultado in resultados) {
            var esperado = admitidas.Contains(resultado.Letra);
            resultado.CreateValido.Should().Be(esperado, $"letra {resultado.Letra} en posición {posicion + 1}");
            resultado.UpdateValido.Should().Be(esperado);
            resultado.CreateMapeable.Should().Be(esperado);
            resultado.UpdateMapeable.Should().Be(esperado);
        }
    }

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
        Color = "Azul",
        PrecioBaseDia = 45.90m,
        Plazas = 5,
        MaleteroLitros = 361,
        AutonomiaKm = 780,
        Kilometraje = 12_500,
        FechaUltimaRevision = "2026-10-01",
        Concesionario = 10L
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
            Id = "vehiculo-01",
            FotoFk = "delantera.jpg",
            Foto = CrearFoto(),
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

    [TestCase("no-es-una-fecha")]
    [TestCase("9999-01-01")]
    [TestCase("2026-02-30")]
    public void Fecha_InvalidaOFutura_ValidadoresYMapeadoresLaRechazan(string texto)
    {
        // Arrange
        var dto = CrearDto() with { FechaUltimaRevision = texto };
        var update = CrearUpdateDto() with { FechaUltimaRevision = texto };

        // Act
        var resultado = dto.ToVehiculo(CrearFoto());
        var modificacion = update.ToVehiculoModel(CrearFoto());
        var altaValida = new CreateVehiculoValidator().Validate(dto);
        var modificacionValida = new UpdateVehiculoValidator().Validate(update);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<Alquiler_Vehiculos.Errors.Common.ValidationError>();
        modificacion.IsFailure.Should().BeTrue();
        modificacion.Error.Should().BeEquivalentTo(resultado.Error);
        altaValida.Errors.Should().ContainSingle(e => e.PropertyName == "FechaUltimaRevision");
        modificacionValida.Errors.Should().ContainSingle(e => e.PropertyName == "FechaUltimaRevision");
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
    [TestCase('H', TipoCarroceria.Furgoneta)]
    [TestCase('R', TipoCarroceria.Furgoneta)]
    [TestCase('F', TipoCarroceria.Suv)]
    [TestCase('G', TipoCarroceria.Suv)]
    [TestCase('J', TipoCarroceria.TodoTerreno)]
    [TestCase('P', TipoCarroceria.TodoTerreno)]
    [TestCase('Q', TipoCarroceria.TodoTerreno)]
    [TestCase('T', TipoCarroceria.Descapotable)]
    [TestCase('N', TipoCarroceria.Descapotable)]
    [TestCase('E', TipoCarroceria.Coupe)]
    [TestCase('S', TipoCarroceria.Coupe)]
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
    /// (solo los códigos comerciales <c>Especial</c> y <c>OfertaEspecial</c>).
    /// </summary>
    private static readonly char[] LetrasSinCarroceria = ['X', 'Z'];

    [TestCase('A')]
    [TestCase('I')]
    [TestCase('O')]
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
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeEquivalentTo(VehiculoError.TipoDeCarroceria());
    }

    [Test]
    public void ToVehiculo_ConCualquierAcrissTipoDelCatalogo_SoloFallaSiEstaFueraDelMapaDeCarrocerias()
    {
        // Arrange
        var tipos = Enum.GetValues<AcrissTipo>();

        // Act
        var sinCarroceria = tipos
            .Select(tipo => (Tipo: (char)tipo, Resultado: CrearDto(CrearAcriss((char)tipo)).ToVehiculo(CrearFoto())))
            .Where(x => x.Resultado.IsFailure &&
                x.Resultado.Error.Code == VehiculoError.TipoDeCarroceria().Code &&
                x.Resultado.Error.Message == VehiculoError.TipoDeCarroceria().Message)
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
    [TestCase('Q', TipoTransmision.Automatica)]      // AutomatizacionNivel3
    [TestCase('H', TipoTransmision.Automatica)]      // AutomatizacionNivel4
    [TestCase('F', TipoTransmision.Automatica)]      // AutomatizacionNivel5
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

    [TestCase('V')] // letra fuera del catálogo
    [TestCase('Y')] // letra fuera del catálogo
    public void ToVehiculo_ConTerceraLetraFueraDelCatalogo_DevuelveErrorDeTransmision(char letra)
    {
        // Arrange
        var dto = CrearDto(CrearAcriss('L', letra));

        // Act
        var resultado = dto.ToVehiculo(CrearFoto());

        // Assert
        resultado.Error.Should().BeEquivalentTo(VehiculoError.TipoDeTransmision());
    }

    [Test]
    public void ToVehiculo_ConCualquierTransmisionDelCatalogo_NoFallaPorTransmision()
    {
        // Arrange
        var transmisiones = Enum.GetValues<AcrissTransmisionTraccion>();

        // Act
        var fallidas = transmisiones
            .Select(transmision => (
                Letra: (char)transmision,
                Resultado: CrearDto(CrearAcriss('L', (char)transmision)).ToVehiculo(CrearFoto())))
            .Where(x => x.Resultado.IsFailure && x.Resultado.Error.Code == VehiculoError.TipoDeTransmision().Code
                && x.Resultado.Error.Message == VehiculoError.TipoDeTransmision().Message)
            .Select(x => x.Letra)
            .ToArray();

        // Assert
        fallidas.Should().BeEmpty(
            "todas las transmisiones del catálogo, incluidas las automatizaciones 3/4/5, tienen equivalente");
    }

    [TestCase('D', TipoCombustible.Diesel)]    // DieselConAire
    [TestCase('Q', TipoCombustible.Diesel)]    // DieselSinAire
    [TestCase('V', TipoCombustible.Gasolina)]  // GasolinaConAire
    [TestCase('Z', TipoCombustible.Gasolina)]  // GasolinaSinAire
    [TestCase('H', TipoCombustible.Hibrido)]   // Hibrido
    [TestCase('I', TipoCombustible.Hibrido)]   // HibridoEnchufable
    [TestCase('E', TipoCombustible.Electrico)] // ElectricoE
    [TestCase('C', TipoCombustible.Electrico)] // ElectricoC
    [TestCase('A', TipoCombustible.Electrico)] // HidrogenoConAire (pila de combustible)
    [TestCase('B', TipoCombustible.Electrico)] // HidrogenoSinAire (pila de combustible)
    [TestCase('L', TipoCombustible.Gasolina)]  // GlpConAire
    [TestCase('S', TipoCombustible.Gasolina)]  // GlpSinAire
    [TestCase('M', TipoCombustible.Gasolina)]  // MulticombustibleConAire
    [TestCase('F', TipoCombustible.Gasolina)]  // MulticombustibleSinAire
    [TestCase('U', TipoCombustible.Gasolina)]  // EtanolConAire
    [TestCase('X', TipoCombustible.Gasolina)]  // EtanolSinAire
    [TestCase('R', TipoCombustible.Gasolina)]  // SinEspecificarConAire (ejemplo CCMR)
    [TestCase('N', TipoCombustible.Gasolina)]  // SinEspecificarSinAire
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

    [TestCase('K')] // letra fuera del catálogo
    [TestCase('Y')] // letra fuera del catálogo
    public void ToVehiculo_ConCuartaLetraFueraDelCatalogo_DevuelveErrorDeCombustible(char letra)
    {
        // Arrange
        var dto = CrearDto(CrearAcriss('L', 'A', letra));

        // Act
        var resultado = dto.ToVehiculo(CrearFoto());

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeEquivalentTo(VehiculoError.TipoCombustible());
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
        var dto = CrearUpdateDto(CrearAcriss('X'));

        // Act
        var resultado = dto.ToVehiculoModel(CrearFoto());

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeEquivalentTo(VehiculoError.TipoDeCarroceria());
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
            Id = "vehiculo-01",
            FotoRuta = "/uploads/vehiculos/delantera.jpg",
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
    public void ToDto_ConModelo_IncluyeElIdGeneradoPorElServidor()
    {
        // Arrange
        var modelo = CrearModelo();

        // Act
        var dto = modelo.ToDto();

        // Assert — RF-01 y RF-06: el cliente necesita el identificador de 11 caracteres.
        dto.Id.Should().Be(modelo.Id);
        dto.Id.Should().HaveLength(11);
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
