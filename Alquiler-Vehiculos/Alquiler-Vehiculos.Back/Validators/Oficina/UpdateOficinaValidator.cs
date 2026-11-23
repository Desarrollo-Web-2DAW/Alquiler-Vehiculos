using Alquiler_Vehiculos.Dto.Oficina;
using FluentValidation;
using OficinaEntity = Alquiler_Vehiculos.Models.Oficina.Oficina;

namespace Alquiler_Vehiculos.Validators.Oficina;

/// <summary>
/// Reglas de formato de actualización de la oficina y su horario semanal (RF-11).
/// </summary>
public class UpdateOficinaValidator : AbstractValidator<UpdateOficinaDto> {
    public UpdateOficinaValidator() {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la oficina es obligatorio")
            .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres")
            .MaximumLength(150).WithMessage("El nombre no puede superar los 150 caracteres");

        RuleFor(x => x.Direccion)
            .NotEmpty().WithMessage("La dirección es obligatoria")
            .MinimumLength(5).WithMessage("La dirección debe tener al menos 5 caracteres")
            .MaximumLength(250).WithMessage("La dirección no puede superar los 250 caracteres");

        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El teléfono es obligatorio")
            .Matches(@"^[0-9+ ]{9,20}$").WithMessage("El teléfono no tiene un formato válido");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("Debe ser un email válido")
            .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$")
                .WithMessage("El email debe tener un dominio válido (por ejemplo, oficina@empresa.es)")
            .MaximumLength(150).WithMessage("El email no puede superar los 150 caracteres");

        RuleFor(x => x.DiaInicio)
            .IsInEnum().WithMessage("El día de inicio no es válido");

        RuleFor(x => x.DiaFin)
            .IsInEnum().WithMessage("El día de fin no es válido");

        RuleFor(x => x)
            .Must(x => OficinaEntity.OrdenSemana(x.DiaFin) >= OficinaEntity.OrdenSemana(x.DiaInicio))
            .WithName("dias")
            .WithMessage("El día de fin no puede ser anterior al día de inicio");

        RuleFor(x => x)
            .Must(x => x.HoraCierre > x.HoraApertura)
            .WithName("horario")
            .WithMessage("La hora de cierre debe ser posterior a la hora de apertura");
    }
}
