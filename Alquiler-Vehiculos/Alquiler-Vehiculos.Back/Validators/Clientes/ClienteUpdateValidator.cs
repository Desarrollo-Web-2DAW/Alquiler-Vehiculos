using Alquiler_Vehiculos.Dto.Clientes;
using Alquiler_Vehiculos.Validators.Documentos;
using FluentValidation;

namespace Alquiler_Vehiculos.Validators.Clientes;

/// <summary>
/// Reglas de formato de la modificación de un cliente (RF-13, RF-14).
/// Son las del alta menos el correo electrónico, que identifica al cliente y no se cambia.
/// No consulta la base de datos: los duplicados y el estado los comprueba el servicio.
/// </summary>
/// <remarks>
/// La mayoría de edad no se comprueba aquí: se mide el día de la recogida del alquiler.
/// </remarks>
public class ClienteUpdateValidator : AbstractValidator<ClienteUpdateDto> {
    private const int LongitudMaximaPermiso = 20;
    private static readonly DateOnly FechaNacimientoMinima = new(1900, 1, 1);

    /// <summary>Define las reglas de la modificación.</summary>
    public ClienteUpdateValidator() {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres")
            .MaximumLength(100).WithMessage("El nombre no puede tener más de 100 caracteres");

        RuleFor(x => x.Apellidos)
            .NotEmpty().WithMessage("Los apellidos son obligatorios")
            .MinimumLength(2).WithMessage("Los apellidos deben tener al menos 2 caracteres")
            .MaximumLength(150).WithMessage("Los apellidos no pueden tener más de 150 caracteres");

        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El teléfono es obligatorio")
            .MinimumLength(9).WithMessage("El teléfono debe tener al menos 9 caracteres")
            .MaximumLength(20).WithMessage("El teléfono no puede tener más de 20 caracteres")
            .Must(t => string.IsNullOrEmpty(t) || t.All(c => (c >= '0' && c <= '9') || c == '+' || c == ' '))
            .WithMessage("El teléfono solo puede llevar dígitos, el signo + y espacios");

        RuleFor(x => x.FechaNacimiento)
            .Must(fecha => fecha >= FechaNacimientoMinima)
            .WithMessage("La fecha de nacimiento es obligatoria y no puede ser anterior a 1900")
            .Must(fecha => fecha < Hoy())
            .WithMessage("La fecha de nacimiento debe ser anterior a hoy");

        RuleFor(x => x.Dni)
            .Must(dni => DniValidator.EsValido(dni))
            .When(x => !string.IsNullOrWhiteSpace(x.Dni))
            .WithMessage("El DNI no es válido: ocho dígitos y la letra de control que le corresponde");

        RuleFor(x => x.Nie)
            .Must(nie => NieValidator.EsValido(nie))
            .When(x => !string.IsNullOrWhiteSpace(x.Nie))
            .WithMessage("El NIE no es válido: X, Y o Z, siete dígitos y la letra de control que le corresponde");

        RuleFor(x => x.Pasaporte)
            .Must(pasaporte => PasaporteValidator.EsValido(pasaporte))
            .When(x => !string.IsNullOrWhiteSpace(x.Pasaporte))
            .WithMessage("El pasaporte no es válido: nueve caracteres del número y su dígito de control");

        RuleFor(x => x.Dni)
            .NotEmpty()
            .When(x => string.IsNullOrWhiteSpace(x.Nie) && string.IsNullOrWhiteSpace(x.Pasaporte))
            .WithMessage("Debe indicar al menos un documento: DNI, NIE o pasaporte");

        RuleFor(x => x.NumeroPermisoConducir)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.Pasaporte)
                       && string.IsNullOrWhiteSpace(x.Dni)
                       && string.IsNullOrWhiteSpace(x.Nie))
            .WithMessage("El número del permiso de conducir es obligatorio si solo se identifica con pasaporte");

        RuleFor(x => x.NumeroPermisoConducir)
            .MaximumLength(LongitudMaximaPermiso)
            .WithMessage($"El número del permiso de conducir no puede tener más de {LongitudMaximaPermiso} caracteres");
    }

    private static DateOnly Hoy() {
        var ahora = DateTime.UtcNow;
        return new DateOnly(ahora.Year, ahora.Month, ahora.Day);
    }
}