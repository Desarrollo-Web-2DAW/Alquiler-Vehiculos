using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Models.Vehiculo.Enums.ACRISS;
using FluentValidation;
using Alquiler_Vehiculos.Mappers.Vehiculo;

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
            .NotEmpty().WithMessage("La foto es obligatoria.");
        
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

        // Cascade Stop: si la longitud o el formato fallan, no tiene sentido evaluar letra a letra.
        RuleFor(v => v.Acriss)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El código ACRISS no puede estar vacío.")
            .Matches(_patronAcriss).WithMessage("El código ACRISS debe constar exactamente de 4 letras.")
            .Must(ValidarCodigoAcriss).WithMessage("El código ACRISS contiene valores no válidos en sus letras.");
            
        RuleFor(v => v.Estado)
            .NotEmpty().WithMessage("El estado no puede estar vacío.")
            .Must(e => !int.TryParse(e, out _) && Enum.TryParse<EstadoVehiculo>(e, true, out var estado)
                && Enum.IsDefined(estado) && estado != EstadoVehiculo.Retirado)
            .WithMessage("El estado del vehículo no es válido. Para retirarlo, usa la baja lógica.");

        RuleFor(v => v.Color)
            .NotEmpty().WithMessage("El color no puede estar vacío.")
            .Must(col => !int.TryParse(col, out _) && Enum.TryParse<TipoColor>(col, true, out var color)
                && Enum.IsDefined(color))
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
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("La fecha de la última revisión no puede estar vacía.")
            .Must(texto => DateTime.TryParse(texto, out var fecha) && fecha <= DateTime.UtcNow)
            .WithMessage("La fecha de la última revisión debe ser válida y no puede ser futura.");

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
        bool esTipoValido = VehiculoMapper.ObtenerTipoCarroceria(c2).IsSuccess;
        bool esTransValida = VehiculoMapper.ObtenerTipoTransmision(c3).IsSuccess;
        bool esCombValido = VehiculoMapper.ObtenerTipoCombustible(c4).IsSuccess;

        return esCatValida && esTipoValido && esTransValida && esCombValido;
    }
}
