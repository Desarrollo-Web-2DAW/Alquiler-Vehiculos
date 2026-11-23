using Alquiler_Vehiculos.Dto.Oficina;
using FluentValidation;

namespace Alquiler_Vehiculos.Validators.Oficina;

/// <summary>
/// Valida que la petición de relevo de responsable indique un identificador válido (CU-18).
/// </summary>
public class CambiarResponsableValidator : AbstractValidator<CambiarResponsableDto> {
    public CambiarResponsableValidator() {
        RuleFor(x => x.TrabajadorId)
            .NotEmpty().WithMessage("El identificador del nuevo responsable es obligatorio");
    }
}
