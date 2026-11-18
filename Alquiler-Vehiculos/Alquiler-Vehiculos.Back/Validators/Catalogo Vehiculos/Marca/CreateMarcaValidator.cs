using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Marca;
using FluentValidation;

namespace Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Marca;

/// <summary>
/// Valida los datos de entrada para crear una marca con sus modelos disponibles.
/// </summary>
/// <remarks>
/// Exige nombre, país y web de entre 2 y 100 caracteres, al menos un modelo
/// disponible y nombres de modelos que no sean nulos, vacíos ni solo espacios.
/// La regla de web comprueba contenido y longitud, pero no el formato de URL.
/// </remarks>
public class CreateMarcaValidator : AbstractValidator<CreateMarcaDto> {

    /// <summary>
    /// Inicializa las reglas del nombre, el país, la web, la colección de modelos
    /// disponibles y cada nombre contenido en ella.
    /// </summary>
    public CreateMarcaValidator() {
        RuleFor(m => m.Nombre)
            .NotEmpty().WithMessage("El nombre no puede estar vacío.")
            .MaximumLength(100).WithMessage("El nombre no puede tener más de 100 caracteres.")
            .MinimumLength(2).WithMessage("El nombre no puede tener menos de 2 caracteres.");
        
        RuleFor(m => m.Pais)
            .NotEmpty().WithMessage("El pais no puede estar vacío.")
            .MaximumLength(100).WithMessage("El pais no puede tener más de 100 caracteres.")
            .MinimumLength(2).WithMessage("El pais no puede tener menos de 2 caracteres.");
        
        RuleFor(m => m.Web)
            .NotEmpty().WithMessage("La web no puede estar vacía.")
            .MaximumLength(100).WithMessage("La web no puede tener más de 100 caracteres.")
            .MinimumLength(2).WithMessage("La web no puede tener menos de 2 caracteres.");
        
        RuleFor(m => m.ModelosDisponibles)
            .NotEmpty()
            .WithMessage("La marca debe tener al menos un modelo disponible");

        RuleForEach(m => m.ModelosDisponibles)
            .NotEmpty()
            .WithMessage("El nombre de cada modelo no puede estar vacio.");
    }
}
