using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Models.Vehiculo.Enums.ACRISS;
using FluentValidation;

namespace Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Vehiculo;

/// <summary>
/// Valida los datos de entrada para actualizar un vehiculo.
/// </summary>
public class UpdateVehiculoValidator : AbstractValidator<UpdateVehiculoDto>
{
    // Patrón estándar para matrículas españolas modernas (4 números y 3 letras, sin consonantes disonantes)
    private static readonly string _patronMatricula = "^[0-9]{4}[BCDFGHJKLMNPRSTVWXYZ]{3}$";
    
    // Patrón opcional para el código ACRISS de 4 letras exactas
    private static readonly string _patronAcriss = "^[A-Z]{4}$";

    public UpdateVehiculoValidator() 
    {
        RuleFor(v => v.Foto)
            .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .When(v => !string.IsNullOrEmpty(v.Foto))
            .WithMessage("La imagen debe ser una URL válida.");
        
        RuleFor(m => m.Matricula)
            .NotEmpty().WithMessage("La matrícula no puede estar vacía.")
            .Matches(_patronMatricula).WithMessage("La matrícula debe tener un formato válido (4 números y 3 letras).");
        
        RuleFor(m => m.Marca)
            .NotEmpty().WithMessage("La marca no puede estar vacía.")
            .MaximumLength(100).WithMessage("La marca no puede tener más de 100 caracteres.")
            .MinimumLength(2).WithMessage("La marca no puede tener menos de 2 caracteres.");
        
        RuleFor(m => m.Modelo)
            .NotEmpty().WithMessage("El modelo no puede estar vacío.")
            .MaximumLength(100).WithMessage("El modelo no puede tener más de 100 caracteres.")
            .MinimumLength(2).WithMessage("El modelo no puede tener menos de 2 caracteres.");

        RuleFor(v => v.Acriss)
            .NotEmpty().WithMessage("El código ACRISS no puede estar vacío.")
            .Matches(_patronAcriss).WithMessage("El código ACRISS debe constar exactamente de 4 letras.")
            .Must(ValidarCodigoAcriss).WithMessage("El código ACRISS contiene valores no válidos en sus letras.");
            
        RuleFor(v => v.Estado)
            .NotEmpty().WithMessage("El estado no puede estar vacío.")
            .Must(e => Enum.TryParse<EstadoVehiculo>(e, true, out _))
            .WithMessage("El estado del vehículo no es válido.");

        RuleFor(v => v.Color)
            .NotEmpty().WithMessage("El color no puede estar vacío.")
            .Must(col => Enum.TryParse<TipoColor>(col, true, out _))
            .WithMessage("El color seleccionado no es válido.");

        RuleFor(v => v.PrecioBaseDia)
            .GreaterThan(0).WithMessage("El precio base por día debe ser mayor que cero.");

        RuleFor(v => v.Plazas)
            .InclusiveBetween(2, 9).WithMessage("El número de plazas debe estar entre 2 y 9.");

        RuleFor(v => v.MaleteroLitros)
            .InclusiveBetween(0, 6000).WithMessage("La capacidad del maletero debe estar entre 0 y 6.000 litros.");

        RuleFor(v => v.AutonomiaKm)
            .GreaterThan(0).WithMessage("La autonomía en kilómetros debe ser mayor que cero.");

        RuleFor(v => v.Kilometraje)
            .GreaterThanOrEqualTo(0).WithMessage("El kilometraje no puede ser negativo.");

        RuleFor(v => v.FechaUltimaRevision)
            .NotEmpty().WithMessage("La fecha de la última revisión no puede estar vacía.");

        RuleFor(v => v.Concesionario)
            .GreaterThan(0).WithMessage("El identificador del concesionario debe ser válido.");
    }

    /// <summary>
    /// Valida que cada una de las 4 letras del código ACRISS pertenezca 
    /// a sus respectivos enums de definición (Categoría, Tipo, Transmisión y Combustible/Climatización).
    /// </summary>
    private bool ValidarCodigoAcriss(string acriss)
    {
        if (string.IsNullOrEmpty(acriss) || acriss.Length != 4)
            return false;

        char c1 = char.ToUpper(acriss[0]);
        char c2 = char.ToUpper(acriss[1]);
        char c3 = char.ToUpper(acriss[2]);
        char c4 = char.ToUpper(acriss[3]);

        bool esCatValida = Enum.IsDefined(typeof(AcrissCategoria), (int)c1);
        bool esTipoValido = Enum.IsDefined(typeof(AcrissTipo), (int)c2);
        bool esTransValida = Enum.IsDefined(typeof(AcrissTransmisionTraccion), (int)c3);
        bool esCombValido = Enum.IsDefined(typeof(AcrissCombustibleClimatizacion), (int)c4);

        return esCatValida && esTipoValido && esTransValida && esCombValido;
    }
}