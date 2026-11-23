using Alquiler_Vehiculos.Dto.Taller;
using FluentValidation;

namespace Alquiler_Vehiculos.Validators.Taller;

/// <summary>
/// Reglas de formato de la apertura de un parte de taller (RF-08).
/// El identificador del vehículo es el corto de 11 caracteres del servidor (RF-01).
/// </summary>
public class AbrirParteValidator : AbstractValidator<AbrirParteDto> {
    public AbrirParteValidator() {
        RuleFor(x => x.VehiculoId)
            .NotEmpty().WithMessage("El vehículo es obligatorio")
            .Length(11).WithMessage("El identificador del vehículo debe tener 11 caracteres (RF-01)");

        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción del trabajo es obligatoria")
            .MaximumLength(1000).WithMessage("La descripción no puede superar los 1000 caracteres");
    }
}
