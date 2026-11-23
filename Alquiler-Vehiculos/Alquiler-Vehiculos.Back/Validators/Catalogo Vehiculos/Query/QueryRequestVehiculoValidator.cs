using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Query;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using FluentValidation;

namespace Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Query;

/// <summary>
/// Valida los filtros opcionales y el rango de fechas de la búsqueda de vehículos.
/// No consulta reservas ni comprueba el horario de oficina.
/// </summary>
public class QueryRequestVehiculoValidator : AbstractValidator<QueryRequestVehiculoDto> {
    
    /// <summary>
    /// Define las clasificaciones admitidas, los mínimos numéricos y las reglas de fechas.
    /// Los filtros no proporcionados no limitan la búsqueda.
    /// </summary>
    public QueryRequestVehiculoValidator() {
        RuleFor(v => v.Categoria)
            .Must(texto => !int.TryParse(texto, out _)
                && Enum.TryParse<TipoCarroceria>(texto, true, out var categoria)
                && Enum.IsDefined(categoria))
            .WithMessage("La categoría indicada no es válida.")
            .When(v => !string.IsNullOrEmpty(v.Categoria));

        RuleFor(v => v.Estado)
            .Must(texto => !int.TryParse(texto, out _)
                && Enum.TryParse<EstadoVehiculo>(texto, true, out var estado)
                && Enum.IsDefined(estado))
            .WithMessage("El estado indicado no es válido.")
            .When(v => !string.IsNullOrEmpty(v.Estado));

        RuleFor(v => v.Combustible)
            .Must(texto => !int.TryParse(texto, out _)
                && Enum.TryParse<TipoCombustible>(texto, true, out var combustible)
                && Enum.IsDefined(combustible))
            .WithMessage("El combustible indicado no es válido.")
            .When(v => !string.IsNullOrEmpty(v.Combustible));

        RuleFor(v => v.Transmision)
            .Must(texto => !int.TryParse(texto, out _)
                && Enum.TryParse<TipoTransmision>(texto, true, out var transmision)
                && Enum.IsDefined(transmision))
            .WithMessage("La transmisión indicada no es válida.")
            .When(v => !string.IsNullOrEmpty(v.Transmision));

        RuleFor(v => v.PlazasMinimas)
            .GreaterThan(0).WithMessage("Las plazas mínimas deben ser mayores que cero.")
            .When(v => v.PlazasMinimas.HasValue);

        RuleFor(v => v.MaleteroLitrosMinimos)
            .GreaterThanOrEqualTo(0).WithMessage("Los litros mínimos de maletero no pueden ser negativos.")
            .When(v => v.MaleteroLitrosMinimos.HasValue);

        RuleFor(v => v.AutonomiaKmMinima)
            .GreaterThan(0).WithMessage("La autonomía mínima debe ser mayor que cero.")
            .When(v => v.AutonomiaKmMinima.HasValue);

        // Si se envía una fecha, la otra también es obligatoria.
        RuleFor(v => v.FechaRecogida)
            .NotNull().WithMessage("Debes indicar la fecha de recogida junto a la de devolución.")
            .When(v => v.FechaDevolucion.HasValue);

        RuleFor(v => v.FechaDevolucion)
            .NotNull().WithMessage("Debes indicar la fecha de devolución junto a la de recogida.")
            .When(v => v.FechaRecogida.HasValue);

        RuleFor(v => v.FechaRecogida)
            .Must(fecha => !fecha.HasValue || fecha.Value >= DateTime.UtcNow)
            .WithMessage("La recogida no puede quedar en el pasado.");

        // Los días se cuentan de fecha a fecha, sin usar las horas para calcular la duración.
        RuleFor(v => v.FechaDevolucion)
            .Must((request, fecha) => {
                var dias = (fecha!.Value.Date - request.FechaRecogida!.Value.Date).Days;
                return dias >= 1 && dias <= 30;
            })
            .WithMessage("El alquiler debe durar entre 1 y 30 días, contados de fecha a fecha.")
            .When(v => v.FechaRecogida.HasValue && v.FechaDevolucion.HasValue);
    }
}