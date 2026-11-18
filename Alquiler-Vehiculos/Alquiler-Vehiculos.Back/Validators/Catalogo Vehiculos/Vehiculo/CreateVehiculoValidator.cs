using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using FluentValidation;

namespace Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Vehiculo;

public class CreateVehiculoValidator : AbstractValidator<CreateVehiculoDto> {
    public CreateVehiculoValidator() {
        RuleFor(v => v.Foto)
            .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .When(v => !string.IsNullOrEmpty(v.Foto))
            .WithMessage("La imagen debe ser una URL válida");
    }
}