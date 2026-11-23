using Alquiler_Vehiculos.Dto.Taller;
using Alquiler_Vehiculos.Validators.Taller;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Taller;

/// <summary>
/// Tests unitarios del validador de apertura de partes (AbrirParteValidator).
/// Requisito cubierto: RF-08 (datos del parte) y RF-01 (el identificador del
/// vehículo es el corto de 11 caracteres).
/// </summary>
[Category("Unit")]
public class AbrirParteValidatorTests {
    private readonly AbrirParteValidator _validador = new();

    /// <summary>
    /// Dado que los datos cumplen el formato, cuando se validan,
    /// entonces no devuelven errores.
    /// </summary>
    [Test]
    public void Validate_DatosValidos_NoDevuelveErrores() {
        // Arrange
        var dto = new AbrirParteDto { VehiculoId = "VEHICULO001", Descripcion = "Cambio de frenos" };

        // Act
        var resultado = _validador.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Dado que falta el vehículo, cuando se valida,
    /// entonces devuelve el error de obligatorio.
    /// </summary>
    [Test]
    public void Validate_VehiculoVacio_DevuelveErrorDeObligatorio() {
        // Arrange
        var dto = new AbrirParteDto { VehiculoId = "", Descripcion = "Cambio de frenos" };

        // Act
        var resultado = _validador.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == "VehiculoId" && e.ErrorMessage.Contains("obligatorio"));
    }

    /// <summary>
    /// Dado que el identificador del vehículo no tiene 11 caracteres, cuando se valida,
    /// entonces devuelve el error de longitud (RF-01).
    /// </summary>
    [Test]
    public void Validate_VehiculoConLongitudDistinta_DevuelveErrorDeLongitud() {
        // Arrange
        var dto = new AbrirParteDto { VehiculoId = "corto", Descripcion = "Cambio de frenos" };

        // Act
        var resultado = _validador.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == "VehiculoId" && e.ErrorMessage.Contains("11 caracteres"));
    }

    /// <summary>
    /// Dado que falta la descripción, cuando se valida,
    /// entonces devuelve el error de obligatorio.
    /// </summary>
    [Test]
    public void Validate_DescripcionVacia_DevuelveErrorDeObligatorio() {
        // Arrange
        var dto = new AbrirParteDto { VehiculoId = "VEHICULO001", Descripcion = "  " };

        // Act
        var resultado = _validador.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == "Descripcion" && e.ErrorMessage.Contains("obligatoria"));
    }

    /// <summary>
    /// Dado que la descripción es demasiado larga, cuando se valida,
    /// entonces devuelve el error de longitud máxima.
    /// </summary>
    [Test]
    public void Validate_DescripcionDemasiadoLarga_DevuelveErrorDeLongitud() {
        // Arrange
        var dto = new AbrirParteDto { VehiculoId = "VEHICULO001", Descripcion = new string('x', 1001) };

        // Act
        var resultado = _validador.Validate(dto);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == "Descripcion" && e.ErrorMessage.Contains("1000"));
    }
}
