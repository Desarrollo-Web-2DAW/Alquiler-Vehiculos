using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Vehiculo;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators;

[TestFixture]
[Category("Unit")]
public class UpdateVehiculoValidatorTests {
    private UpdateVehiculoValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateVehiculoValidator();

    private static UpdateVehiculoDto CrearDtoValido() => new() {
        Foto = "https://example.com/coche.jpg", Matricula = "1234BCD",
        Marca = "Toyota", Modelo = "Corolla", Acriss = "ILAH", Estado = "Disponible",
        Color = "Azul", PrecioBaseDia = 45.50m, Plazas = 5, MaleteroLitros = 361,
        AutonomiaKm = 700, Kilometraje = 12000, FechaUltimaRevision = "2026-11-01",
        Concesionario = 10L, IsActivo = true
    };

    [TestCase(null)]
    [TestCase("")]
    [TestCase("https://example.com/coche.jpg")]
    [TestCase("http://example.com/coche.png?size=100")]
    [TestCase("ftp://example.com/coche.jpg")]
    public void Validate_FotoOpcionalOUrlAbsoluta_NoDevuelveErrores(string? foto) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Foto = foto!;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [TestCase("   ")]
    [TestCase("coche.jpg")]
    [TestCase("www.example.com/coche.jpg")]
    public void Validate_FotoRelativaOInvalida_DevuelveErrorDeFoto(string foto) {
        // Arrange
        var dto = CrearDtoValido();
        dto.Foto = foto;

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle(e => e.PropertyName == "Foto"
            && e.ErrorMessage == "La imagen debe ser una URL válida.");
    }

    [TestCase("Matricula", "1234BCD", true)]
    [TestCase("Matricula", "0000ZZZ", true)]
    [TestCase("Matricula", "123BCD", false)]
    [TestCase("Matricula", "1234ABC", false)]
    [TestCase("Matricula", "1234bcd", false)]
    [TestCase("Matricula", "1234 BCD", false)]
    [TestCase("Acriss", "ILAH", true)]
    [TestCase("Acriss", "CDMR", true)]
    [TestCase("Acriss", "ilah", false)]
    [TestCase("Acriss", "ILA", false)]
    [TestCase("Acriss", "ILAHH", false)]
    [TestCase("Acriss", "ZLAH", false)]
    [TestCase("Acriss", "IUAH", false)]
    [TestCase("Acriss", "ILZH", false)]
    [TestCase("Acriss", "ILAU", false)]
    [TestCase("Estado", "Disponible", true)]
    [TestCase("Estado", "enAlquiler", true)]
    [TestCase("Estado", "Desconocido", false)]
    [TestCase("Color", "Azul", true)]
    [TestCase("Color", "rojo", true)]
    [TestCase("Color", "Inexistente", false)]
    public void Validate_FormatoOEnumeracion_RespetaReglaDelCampo(string campo, string valor, bool valido) {
        // Arrange
        var dto = CrearDtoValido();
        AsignarTexto(dto, campo, valor);

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().Be(valido);
        if (valido) resultado.Errors.Should().BeEmpty();
        else {
            resultado.Errors.Should().NotBeEmpty();
            resultado.Errors.Should().OnlyContain(e => e.PropertyName == campo);
        }
    }

    [TestCase("Matricula", "La matrícula no puede estar vacía.")]
    [TestCase("MarcaModel", "La marcaModel no puede estar vacía.")]
    [TestCase("Modelo", "El modelo no puede estar vacío.")]
    [TestCase("Acriss", "El código ACRISS no puede estar vacío.")]
    [TestCase("Estado", "El estado no puede estar vacío.")]
    [TestCase("Color", "El color no puede estar vacío.")]
    [TestCase("FechaUltimaRevision", "La fecha de la última revisión no puede estar vacía.")]
    public void Validate_CampoObligatorioSinContenido_DevuelveErrorConcreto(string campo, string mensaje) {
        // Arrange
        var dto = CrearDtoValido();

        foreach (var valor in new string?[] { null, "", "   " }) {
            AsignarTexto(dto, campo, valor!);

            // Act
            var resultado = _validator.Validate(dto);

            // Assert
            resultado.IsValid.Should().BeFalse();
            resultado.Errors.Should().Contain(e => e.PropertyName == campo && e.ErrorMessage == mensaje);
            resultado.Errors.Should().OnlyContain(e => e.PropertyName == campo);
        }
    }

    [TestCase("MarcaModel", 1, false)]
    [TestCase("MarcaModel", 2, true)]
    [TestCase("MarcaModel", 100, true)]
    [TestCase("MarcaModel", 101, false)]
    [TestCase("Modelo", 1, false)]
    [TestCase("Modelo", 2, true)]
    [TestCase("Modelo", 100, true)]
    [TestCase("Modelo", 101, false)]
    public void Validate_LongitudDeMarcaOModelo_RespetaLimites(string campo, int longitud, bool valido) {
        // Arrange
        var dto = CrearDtoValido();
        AsignarTexto(dto, campo, new string('a', longitud));
        var nombre = campo == "MarcaModel" ? "La marcaModel" : "El modelo";
        var mensaje = longitud < 2 ? $"{nombre} no puede tener menos de 2 caracteres."
            : $"{nombre} no puede tener más de 100 caracteres.";

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().Be(valido);
        if (valido) resultado.Errors.Should().BeEmpty();
        else resultado.Errors.Should().ContainSingle(e => e.PropertyName == campo && e.ErrorMessage == mensaje);
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
    [TestCase("ConcesionarioModel", -1, false, "El identificador del concesionarioModel debe ser válido.")]
    [TestCase("ConcesionarioModel", 0, false, "El identificador del concesionarioModel debe ser válido.")]
    [TestCase("ConcesionarioModel", 1, true, "")]
    public void Validate_CampoNumerico_RespetaLimites(string campo, int valor, bool valido, string mensaje) {
        // Arrange
        var dto = CrearDtoValido();
        switch (campo) {
            case "PrecioBaseDia": dto.PrecioBaseDia = valor; break;
            case "Plazas": dto.Plazas = valor; break;
            case "MaleteroLitros": dto.MaleteroLitros = valor; break;
            case "AutonomiaKm": dto.AutonomiaKm = valor; break;
            case "Kilometraje": dto.Kilometraje = valor; break;
            case "ConcesionarioModel": dto.Concesionario = valor; break;
        }

        // Act
        var resultado = _validator.Validate(dto);

        // Assert
        resultado.IsValid.Should().Be(valido);
        if (valido) resultado.Errors.Should().BeEmpty();
        else resultado.Errors.Should().ContainSingle(e => e.PropertyName == campo && e.ErrorMessage == mensaje);
    }

    private static void AsignarTexto(UpdateVehiculoDto dto, string campo, string valor) {
        switch (campo) {
            case "Matricula": dto.Matricula = valor; break;
            case "MarcaModel": dto.Marca = valor; break;
            case "Modelo": dto.Modelo = valor; break;
            case "Acriss": dto.Acriss = valor; break;
            case "Estado": dto.Estado = valor; break;
            case "Color": dto.Color = valor; break;
            case "FechaUltimaRevision": dto.FechaUltimaRevision = valor; break;
        }
    }
}
