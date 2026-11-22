using Alquiler_Vehiculos.Dto.Usuarios;
using FluentValidation;

namespace Alquiler_Vehiculos.Validators.Usuarios;

/// <summary>
/// Reglas de formato del login.
/// </summary>
public class LoginValidator : AbstractValidator<LoginDto> {
    public LoginValidator() {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("Debe ser un email válido");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria");
    }
}
