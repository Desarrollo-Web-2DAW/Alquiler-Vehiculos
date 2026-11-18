using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Concesionario;
using FluentValidation;

namespace Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Concesionario;

/// <summary>
/// Valida los datos de entrada para crear un concesionario.
/// </summary>
/// <remarks>
/// Exige nombre y ciudad de entre 2 y 100 caracteres, teléfono obligatorio
/// con el patrón definido y correo electrónico obligatorio con validación
/// básica de formato mediante FluentValidation.
/// </remarks>
public class CreateConcesionarioValidator : AbstractValidator<CreateConcesionarioDto> {
    
    /// <summary>
    /// Patrón de teléfono de nueve cifras que comienza por 6, 7 o 9,
    /// sin prefijo internacional ni separadores.
    /// </summary>
    private static readonly string PatronTelefono = @"\A[679][0-9]{8}\z";
    
    /// <summary>
    /// Inicializa las reglas de validación del nombre, la ciudad,
    /// el teléfono y el correo electrónico del concesionario.
    /// </summary>
    public CreateConcesionarioValidator() {
        RuleFor(c => c.Nombre)
            .NotEmpty().WithMessage("El nombre no puede estar vacío.")
            .MaximumLength(100).WithMessage("El nombre no puede tener más de 100 caracteres.")
            .MinimumLength(2).WithMessage("El nombre no puede tener menos de 2 caracteres.");
        
        RuleFor(c => c.Ciudad)
            .NotEmpty().WithMessage("La ciudad no puede estar vacía.")
            .MaximumLength(100).WithMessage("La ciudad no puede tener más de 100 caracteres.")
            .MinimumLength(2).WithMessage("La ciudad no puede tener menos de 2 caracteres.");
        
        RuleFor(c => c.Telefono)
            .NotEmpty().WithMessage("El teléfono no puede estar vacío.")
            .Matches(PatronTelefono).WithMessage("El teléfono debe tener 9 cifras y empezar por 6, 7 o 9");
        
        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("El email no puede estar vacio.")
            .EmailAddress().WithMessage("El email no tiene un formato válido");
    }
}