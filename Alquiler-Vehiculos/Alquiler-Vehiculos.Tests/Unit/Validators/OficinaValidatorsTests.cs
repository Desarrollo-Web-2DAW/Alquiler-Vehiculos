using Alquiler_Vehiculos.Dto.Oficina;
using Alquiler_Vehiculos.Validators.Oficina;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Validators;

/// <summary>
/// Pruebas unitarias de <see cref="UpdateOficinaValidator"/> y <see cref="CambiarResponsableValidator"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
public class OficinaValidatorsTests {
    private UpdateOficinaValidator _updateValidator = null!;
    private CambiarResponsableValidator _responsableValidator = null!;

    [SetUp]
    public void SetUp() {
        _updateValidator = new UpdateOficinaValidator();
        _responsableValidator = new CambiarResponsableValidator();
    }

    private static UpdateOficinaDto OficinaValida(
        string nombre = "Oficina Central",
        string direccion = "Avenida de la Universidad 1",
        string telefono = "+34 916 000 111",
        string email = "oficina@alquiler.es",
        DayOfWeek diaInicio = DayOfWeek.Monday,
        DayOfWeek diaFin = DayOfWeek.Friday,
        TimeOnly? apertura = null,
        TimeOnly? cierre = null) => new() {
            Nombre = nombre,
            Direccion = direccion,
            Telefono = telefono,
            Email = email,
            DiaInicio = diaInicio,
            DiaFin = diaFin,
            HoraApertura = apertura ?? new TimeOnly(9, 0),
            HoraCierre = cierre ?? new TimeOnly(20, 0)
        };

    [Test]
    public async Task UpdateOficina_DatosValidos_EsValido() {
        // Act
        var resultado = await _updateValidator.ValidateAsync(OficinaValida());

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    [TestCase("no-es-email")]
    [TestCase("pepe@sinpunto")]
    [TestCase("pepe@dom.a")]
    public async Task UpdateOficina_EmailInvalidoOSinDominio_FallaEnEmail(string email) {
        // Act
        var resultado = await _updateValidator.ValidateAsync(OficinaValida(email: email));

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateOficinaDto.Email));
    }

    [Test]
    public async Task UpdateOficina_CierreAnteriorOApertura_FallaEnHorario() {
        // Act
        var resultado = await _updateValidator.ValidateAsync(
            OficinaValida(apertura: new TimeOnly(18, 0), cierre: new TimeOnly(9, 0)));

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "horario");
    }

    [Test]
    public async Task UpdateOficina_DiaFinAnteriorADiaInicio_FallaEnDias() {
        // Act
        var resultado = await _updateValidator.ValidateAsync(
            OficinaValida(diaInicio: DayOfWeek.Friday, diaFin: DayOfWeek.Monday));

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "dias");
    }

    [Test]
    public async Task UpdateOficina_LunesADomingo_EsValido() {
        // Act
        var resultado = await _updateValidator.ValidateAsync(
            OficinaValida(diaInicio: DayOfWeek.Monday, diaFin: DayOfWeek.Sunday));

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task CambiarResponsable_GuidVacio_Falla() {
        // Act
        var resultado = await _responsableValidator.ValidateAsync(new CambiarResponsableDto { TrabajadorId = Guid.Empty });

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CambiarResponsableDto.TrabajadorId));
    }

    [Test]
    public async Task CambiarResponsable_GuidValido_EsValido() {
        // Act
        var resultado = await _responsableValidator.ValidateAsync(new CambiarResponsableDto { TrabajadorId = Guid.NewGuid() });

        // Assert
        resultado.IsValid.Should().BeTrue();
    }
}
