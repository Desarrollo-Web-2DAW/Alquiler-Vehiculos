using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Validators.Documentos;
using FluentValidation;

namespace Alquiler_Vehiculos.Validators.Usuarios;

/// <summary>
/// Reglas de formato del alta de cliente (RF-13, RF-14, RF-16..RF-18).
/// </summary>
public class SignupValidator : AbstractValidator<SignupDto> {
    public SignupValidator() {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("Debe ser un email válido")
            .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$")
                .WithMessage("El email debe tener un dominio válido (por ejemplo, usuario@dominio.com)")
            .MaximumLength(150);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
            .Matches("[A-Z]").WithMessage("La contraseña debe tener al menos una mayúscula")
            .Matches("[a-z]").WithMessage("La contraseña debe tener al menos una minúscula")
            .Matches("[0-9]").WithMessage("La contraseña debe tener al menos un número")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("La contraseña debe tener al menos un símbolo");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100);

        RuleFor(x => x.Apellidos)
            .NotEmpty().WithMessage("Los apellidos son obligatorios")
            .MaximumLength(150);

        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El teléfono es obligatorio")
            .Matches(@"^[0-9+ ]{9,20}$").WithMessage("El teléfono no tiene un formato válido");

        RuleFor(x => x.FechaNacimiento)
            .NotEmpty().WithMessage("La fecha de nacimiento es obligatoria")
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("La fecha de nacimiento debe ser anterior a hoy");

        // RF-14: los documentos son independientes, pero debe venir al menos uno.
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Dni)
                    || !string.IsNullOrWhiteSpace(x.Nie)
                    || !string.IsNullOrWhiteSpace(x.Pasaporte))
            .WithName("documentos")
            .WithMessage("Debe indicar al menos un documento: DNI, NIE o pasaporte");

        RuleFor(x => x.Dni)
            .Must(DniValidator.EsValido)
            .When(x => !string.IsNullOrWhiteSpace(x.Dni))
            .WithMessage("El DNI no tiene un formato o letra de control válida");

        RuleFor(x => x.Nie)
            .Must(NieValidator.EsValido)
            .When(x => !string.IsNullOrWhiteSpace(x.Nie))
            .WithMessage("El NIE no tiene un formato o letra de control válida");

        RuleFor(x => x.Pasaporte)
            .Must(PasaporteValidator.EsValido)
            .When(x => !string.IsNullOrWhiteSpace(x.Pasaporte))
            .WithMessage("El pasaporte no tiene un formato o dígito MRZ válido");
    }
}
