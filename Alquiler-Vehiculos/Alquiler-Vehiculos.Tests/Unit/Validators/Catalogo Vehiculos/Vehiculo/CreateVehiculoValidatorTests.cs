using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Vehiculo;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators;

[TestFixture]
[Category("Unit")]
public class CreateVehiculoValidatorTests {
    private CreateVehiculoValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateVehiculoValidator();

    // Foto es el Nombre de la foto (clave de IFotoRepository), no una URL.
    private static CreateVehiculoDto CrearDtoValido() => new() {
        Foto = "coche.jpg",
        Matricula = "1234BCD", Marca = "Toyota", Modelo = "Corolla", Acriss = "ILAH",
        Color = "Azul", PrecioBaseDia = 45.50m,
        Plazas = 5, MaleteroLitros = 361, AutonomiaKm = 700, Kilometraje = 12000,
        FechaUltimaRevision = "2026-11-01", Concesionario = 10L
    };

    [Test]
    public void Validate_DtoValido_NoDevuelveErrores() {
        // Arrange
        var dto = CrearDtoValido();

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.Errors.Should().BeEmpty();
    }

    [Test]
    public void Validate_DatosValidos_NoDevuelveErrores() {
        // Arrange
        var dto = CrearDtoValido();

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_SinFoto_DevuelveErrorDeFotoObligatoria(string? foto) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Foto = foto!;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.Errors.Should().ContainSingle()
            .Which.Should().Match<FluentValidation.Results.ValidationFailure>(e =>
                e.PropertyName == nameof(dto.Foto) && e.ErrorMessage == "La foto es obligatoria.");
    }

    // Ejemplos reales de la tabla de RF-03 (uno por valor del enum Categoria).
    [TestCase("ILAH")]
    [TestCase("CDMD")]
    [TestCase("IFAE")]
    [TestCase("FJBD")]
    [TestCase("IWMD")]
    [TestCase("PEAV")]
    [TestCase("MTAV")]
    [TestCase("IMMD")]
    [TestCase("FKMD")]
    public void Validate_AcrissDelEnunciado_NoDevuelveErrores(string acriss) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Acriss = acriss;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.Errors.Should().BeEmpty();
    }

    [TestCase("ILA", "El código ACRISS debe constar exactamente de 4 letras.")]
    [TestCase("ILAHX", "El código ACRISS debe constar exactamente de 4 letras.")]
    [TestCase("ilah", "El código ACRISS debe constar exactamente de 4 letras.")]
    [TestCase("ALAH", "El código ACRISS contiene valores no válidos en sus letras.")] // 1ª: A no es categoría
    [TestCase("IAAH", "El código ACRISS contiene valores no válidos en sus letras.")] // 2ª: A no es tipo
    [TestCase("ILVH", "El código ACRISS contiene valores no válidos en sus letras.")] // 3ª: V no es transmisión
    [TestCase("ILAK", "El código ACRISS contiene valores no válidos en sus letras.")] // 4ª: K no es combustible
    public void Validate_AcrissFueraDeCatalogo_DevuelveErrorDeAcriss(string acriss, string mensaje) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Acriss = acriss;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert — CascadeMode.Stop garantiza un único error: el de la primera regla que falla.
        resultado.Errors.Should().ContainSingle()
            .Which.Should().Match<FluentValidation.Results.ValidationFailure>(e =>
                e.PropertyName == nameof(dto.Acriss) && e.ErrorMessage == mensaje);
    }

    [TestCase('F')] // multicombustible sin aire
    [TestCase('U')] // etanol con aire
    public void Validate_CuartaLetraDelCatalogoOficial_EsAceptada(char combustible) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Acriss = $"ILA{combustible}";

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.Errors.Should().BeEmpty();
    }

    [TestCase("1234BCA")] // vocal
    [TestCase("1234BCQ")] // Q
    [TestCase("123BCDF")]
    public void Validate_MatriculaNoValida_DevuelveErrorDeMatricula(string matricula) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Matricula = matricula;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.Errors.Should().ContainSingle(e => e.PropertyName == nameof(dto.Matricula));
    }
    [TestCase("1234BCD")]
    [TestCase("0000ZZZ")]
    public void Validate_MatriculaValida_NoDevuelveErrores(string matricula) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Matricula = matricula;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [TestCase("123BCD")]
    [TestCase("12345BCD")]
    [TestCase("1234ABC")]
    [TestCase("1234ÑBC")]
    [TestCase("1234QBC")]
    [TestCase("1234bcd")]
    [TestCase("1234 BCD")]
    [TestCase(" 1234BCD")]
    public void Validate_MatriculaInvalida_DevuelveErrorDeFormato(string matricula) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Matricula = matricula;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle(e => e.PropertyName == "Matricula"
            && e.ErrorMessage == "La matrícula debe tener un formato válido (4 números y 3 letras).");
    }

    [TestCase("ILAH", true)]
    [TestCase("CDMR", true)]
    [TestCase("ilah", false)]
    [TestCase("ILA", false)]
    [TestCase("ILAHH", false)]
    [TestCase("1LAH", false)]
    [TestCase("ZLAH", false)]
    [TestCase("IUAH", false)]
    [TestCase("ILZH", false)]
    [TestCase("ILAU", false)]
    public void Validate_CodigoAcriss_CompruebaFormatoYCadaPosicion(string acriss, bool valido) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Acriss = acriss;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().Be(valido);
        if (valido) resultado.Errors.Should().BeEmpty();
        else {
            resultado.Errors.Should().NotBeEmpty();
            resultado.Errors.Should().OnlyContain(e => e.PropertyName == "Acriss");
            if (acriss.Length == 4 && acriss.All(char.IsUpper)) {
                resultado.Errors.Should().ContainSingle(e => e.ErrorMessage
                    == "El código ACRISS contiene valores no válidos en sus letras.");
            }
        }
    }

    [TestCase("Color", "Rojo", true)]
    [TestCase("Color", "Verde", true)]
    [TestCase("Color", "Naranja", true)]
    [TestCase("Color", "Amarillo", true)]
    [TestCase("Color", "Blanco", true)]
    [TestCase("Color", "Negro", true)]
    [TestCase("Color", "azul", true)]
    [TestCase("Color", "Fucsia", true)]
    [TestCase("Color", "Inexistente", false)]
    public void Validate_EstadoOColor_CompruebaNombreSinDistinguirMayusculas(
        string campo, string valor, bool valido) {
        // Arrange
        var dto = CrearDtoValido();
        AsignarTexto(dto, campo, valor);
        var mensaje = campo == "Estado"
            ? "El estado del vehículo no es válido."
            : "El color seleccionado no es válido.";

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().Be(valido);
        if (valido) resultado.Errors.Should().BeEmpty();
        else resultado.Errors.Should().ContainSingle(e => e.PropertyName == campo
            && e.ErrorMessage == mensaje);
    }

    [TestCase("Matricula", "La matrícula no puede estar vacía.", null)]
    [TestCase("Matricula", "La matrícula no puede estar vacía.", "")]
    [TestCase("Matricula", "La matrícula no puede estar vacía.", "   ")]
    [TestCase("Marca", "La marca no puede estar vacía.", null)]
    [TestCase("Marca", "La marca no puede estar vacía.", "")]
    [TestCase("Marca", "La marca no puede estar vacía.", "   ")]
    [TestCase("Modelo", "El modelo no puede estar vacío.", null)]
    [TestCase("Modelo", "El modelo no puede estar vacío.", "")]
    [TestCase("Modelo", "El modelo no puede estar vacío.", "   ")]
    [TestCase("Acriss", "El código ACRISS no puede estar vacío.", null)]
    [TestCase("Acriss", "El código ACRISS no puede estar vacío.", "")]
    [TestCase("Acriss", "El código ACRISS no puede estar vacío.", "   ")]
    [TestCase("Color", "El color no puede estar vacío.", null)]
    [TestCase("Color", "El color no puede estar vacío.", "")]
    [TestCase("Color", "El color no puede estar vacío.", "   ")]
    [TestCase("FechaUltimaRevision", "La fecha de la última revisión no puede estar vacía.", null)]
    [TestCase("FechaUltimaRevision", "La fecha de la última revisión no puede estar vacía.", "")]
    [TestCase("FechaUltimaRevision", "La fecha de la última revisión no puede estar vacía.", "   ")]
    public void Validate_CampoObligatorioSinContenido_DevuelveErrorConcreto(
        string campo, string mensaje, string? valor) {
        // Arrange
        var dto = CrearDtoValido();
        AsignarTexto(dto, campo, valor!);

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == campo && e.ErrorMessage == mensaje);
        resultado.Errors.Should().OnlyContain(e => e.PropertyName == campo);
    }

    [TestCase("Marca", 1, false)]
    [TestCase("Marca", 2, true)]
    [TestCase("Marca", 100, true)]
    [TestCase("Marca", 101, false)]
    [TestCase("Modelo", 1, false)]
    [TestCase("Modelo", 2, true)]
    [TestCase("Modelo", 100, true)]
    [TestCase("Modelo", 101, false)]
    public void Validate_LongitudDeMarcaOModelo_RespetaLimites(string campo, int longitud, bool valido) {
        // Arrange
        var dto = CrearDtoValido();
        AsignarTexto(dto, campo, new string('a', longitud));
        var nombre = campo == "Marca" ? "La marca" : "El modelo";
        var mensaje = longitud < 2 ? $"{nombre} no puede tener menos de 2 caracteres."
            : $"{nombre} no puede tener más de 100 caracteres.";

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().Be(valido);
        if (valido) resultado.Errors.Should().BeEmpty();
        else resultado.Errors.Should().ContainSingle(e => e.PropertyName == campo
            && e.ErrorMessage == mensaje);
    }

    [TestCase("PrecioBaseDia", -1, false, "El precio base por día debe ser mayor que cero.")]
    [TestCase("PrecioBaseDia", 0, false, "El precio base por día debe ser mayor que cero.")]
    [TestCase("PrecioBaseDia", 1, true, "")]
    [TestCase("Plazas", 1, false, "El número de plazas debe estar entre 2 y 9.")]
    [TestCase("Plazas", 2, true, "")]
    [TestCase("Plazas", 9, true, "")]
    [TestCase("Plazas", 10, false, "El número de plazas debe estar entre 2 y 9.")]
    [TestCase("MaleteroLitros", -1, false, "La capacidad del maletero debe estar entre 0 y 6.000 litros.")]
    [TestCase("MaleteroLitros", 0, true, "")]
    [TestCase("MaleteroLitros", 6000, true, "")]
    [TestCase("MaleteroLitros", 6001, false, "La capacidad del maletero debe estar entre 0 y 6.000 litros.")]
    [TestCase("AutonomiaKm", -1, false, "La autonomía en kilómetros debe ser mayor que cero.")]
    [TestCase("AutonomiaKm", 0, false, "La autonomía en kilómetros debe ser mayor que cero.")]
    [TestCase("AutonomiaKm", 1, true, "")]
    [TestCase("Kilometraje", -1, false, "El kilometraje no puede ser negativo.")]
    [TestCase("Kilometraje", 0, true, "")]
    [TestCase("Kilometraje", 1, true, "")]
    [TestCase("Concesionario", -1, false, "El identificador del concesionario debe ser válido.")]
    [TestCase("Concesionario", 0, false, "El identificador del concesionario debe ser válido.")]
    [TestCase("Concesionario", 1, true, "")]
    public void Validate_CampoNumerico_RespetaLimites(string campo, int valor, bool valido, string mensaje) {
        // Arrange
        var dto = CrearDtoValido();
        switch (campo) {
            case "PrecioBaseDia": dto.PrecioBaseDia = valor; break;
            case "Plazas": dto.Plazas = valor; break;
            case "MaleteroLitros": dto.MaleteroLitros = valor; break;
            case "AutonomiaKm": dto.AutonomiaKm = valor; break;
            case "Kilometraje": dto.Kilometraje = valor; break;
            case "Concesionario": dto.Concesionario = valor; break;
        }

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().Be(valido);
        if (valido) resultado.Errors.Should().BeEmpty();
        else resultado.Errors.Should().ContainSingle(e => e.PropertyName == campo
            && e.ErrorMessage == mensaje);
    }

    [Test]
    public void Validate_PrecioPositivoMenorQueUno_NoDevuelveErrores() {
        // Arrange
        var dto = CrearDtoValido();
        dto.PrecioBaseDia = 0.01m;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [Test]
    public void CreateVehiculoDto_NoExponeEstadoNiIsActivo() {
        // Arrange
        var tipo = typeof(CreateVehiculoDto);

        // Act
        var propiedades = tipo.GetProperties().Select(p => p.Name);

        // Assert
        propiedades.Should().NotContain(new[] { "Estado", "IsActivo" });
    }

    [Test]
    public void Validate_DtoVacio_DevuelveErroresDeTodosLosCamposObligatorios() {
        // Arrange
        var dto = new CreateVehiculoDto();

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Select(e => e.PropertyName).Distinct().Should().BeEquivalentTo(
            "Matricula", "Marca", "Modelo", "Acriss", "Estado", "Color", "PrecioBaseDia",
            "Plazas", "AutonomiaKm", "FechaUltimaRevision", "Concesionario");
    }

    private static void AsignarTexto(CreateVehiculoDto dto, string campo, string valor) {
        switch (campo) {
            case "Matricula": dto.Matricula = valor; break;
            case "Marca": dto.Marca = valor; break;
            case "Modelo": dto.Modelo = valor; break;
            case "Acriss": dto.Acriss = valor; break;
            case "Color": dto.Color = valor; break;
            case "FechaUltimaRevision": dto.FechaUltimaRevision = valor; break;
        }
    }
}
