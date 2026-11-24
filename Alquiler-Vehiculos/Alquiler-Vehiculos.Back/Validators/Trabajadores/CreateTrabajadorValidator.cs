using Alquiler_Vehiculos.Dto.Trabajadores;
using FluentValidation;

namespace Alquiler_Vehiculos.Validators.Trabajadores;

public class CreateTrabajadorValidator :  AbstractValidator<CreateTrabajadorDto> {
    private const string RegexDni = @"^[0-9]{8}[A-Za-z]$";
    public CreateTrabajadorValidator() {
            RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre no puede estar vacío")
            .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres")
            .MaximumLength(100).WithMessage("El nombre no puede tener más de 100 caracteres");
        RuleFor(x => x.Apellidos)
            .NotEmpty().WithMessage("Los apellidos no pueden estar vacíos")
            .MinimumLength(2).WithMessage("Los apellidos deben tener al menos 2 caracteres")
            .MaximumLength(150).WithMessage("Los apellidos no pueden tener más de 150 caracteres");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("Debe ser un email válido")
            .MaximumLength(150).WithMessage("El email no puede tener más de 150 caracteres");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
            .Matches("[A-Z]").WithMessage("La contraseña debe tener al menos una mayúscula")
            .Matches("[a-z]").WithMessage("La contraseña debe tener al menos una minúscula")
            .Matches("[0-9]").WithMessage("La contraseña debe tener al menos un número")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("La contraseña debe tener al menos un símbolo");
        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El teléfono es obligatorio")
            .Matches(@"^[0-9+ ]{9,20}$").WithMessage("El teléfono no tiene un formato válido");
        RuleFor(x => x.FechaNacimiento)
            .NotEmpty().WithMessage("La fecha de nacimiento es obligatoria")
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-18))
            .WithMessage("El trabajador debe ser mayor de edad")
            .GreaterThan(_ => DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-100))
            .WithMessage("La fecha de nacimiento no es válida");
        RuleFor(x => x.Dni)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El DNI es obligatorio")
            .Matches(RegexDni).WithMessage("El DNI debe tener 8 dígitos y una letra (ej. 12345678Z)")
            .Must(TrabajadorValidator.ValidarLetraDni).WithMessage("La letra del DNI no es válida");
        RuleFor(x => x.FechaIngreso)
            .NotEmpty().WithMessage("La fecha de ingreso es obligatoria")
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("La fecha de ingreso no puede ser futura")
            .GreaterThanOrEqualTo(x => x.FechaNacimiento.AddYears(18))
            .WithMessage("La fecha de ingreso debe ser al menos 18 años posterior al nacimiento");
        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("El tipo de trabajador debe ser Operaciones o Mecanico");
    }
    
   
}