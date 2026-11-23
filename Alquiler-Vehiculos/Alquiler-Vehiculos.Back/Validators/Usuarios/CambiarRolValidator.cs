using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Enums.Usuarios;
using FluentValidation;

namespace Alquiler_Vehiculos.Validators.Usuarios;

/// <summary>
/// Valida que el rol solicitado exista en <see cref="RolUsuario"/> (RF-46).
/// </summary>
public class CambiarRolValidator : AbstractValidator<CambiarRolDto> {
    public CambiarRolValidator() {
        var permitidos = string.Join(", ", Enum.GetNames<RolUsuario>());

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("El rol es obligatorio")
            .Must(rol => !string.IsNullOrWhiteSpace(rol)
                && Enum.TryParse<RolUsuario>(rol.Trim(), ignoreCase: true, out var parsed)
                && Enum.IsDefined(parsed))
            .WithMessage($"El rol no es válido. Valores permitidos: {permitidos}");
    }
}
