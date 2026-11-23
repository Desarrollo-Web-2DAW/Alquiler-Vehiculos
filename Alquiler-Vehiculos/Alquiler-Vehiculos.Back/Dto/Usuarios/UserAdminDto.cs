using System.ComponentModel.DataAnnotations;

namespace Alquiler_Vehiculos.Dto.Usuarios;

/// <summary>
/// Vista administrativa de una cuenta de usuario para gestión por Admin (RF-46).
/// Nunca expone hashes ni sellos internos de Identity.
/// </summary>
public record UserDetalleDto(
    Guid Id,
    string Email,
    string Nombre,
    string Apellidos,
    string Role,
    bool IsDeleted,
    DateTime? DeletedAt,
    Guid? ClienteId,
    Guid? TrabajadorId,
    DateTime CreatedAt);

/// <summary>
/// Petición de cambio de rol de una cuenta por parte de un administrador (RF-46).
/// </summary>
public record CambiarRolDto {
    [Required]
    public string Role { get; init; } = string.Empty;
}
