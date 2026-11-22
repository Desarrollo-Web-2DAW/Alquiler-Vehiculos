using System.ComponentModel.DataAnnotations;

namespace Alquiler_Vehiculos.Dto.Usuarios;

/// <summary>Credenciales de acceso.</summary>
public record LoginDto {
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Alta de un cliente con su cuenta (RF-13, RF-14).
/// Los tres documentos son opcionales e independientes, pero debe venir al menos uno.
/// </summary>
public record SignupDto {
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
        ErrorMessage = "La contraseña debe tener mayúscula, minúscula, número y símbolo")]
    public string Password { get; init; } = string.Empty;

    [Required, MinLength(2), MaxLength(100)]
    public string Nombre { get; init; } = string.Empty;

    [Required, MinLength(2), MaxLength(150)]
    public string Apellidos { get; init; } = string.Empty;

    [Required, MaxLength(20)]
    public string Telefono { get; init; } = string.Empty;

    [Required]
    public DateOnly FechaNacimiento { get; init; }

    [MaxLength(20)] public string? Dni { get; init; }
    [MaxLength(20)] public string? Nie { get; init; }
    [MaxLength(20)] public string? Pasaporte { get; init; }

    /// <summary>Obligatorio solo si el cliente se identifica con pasaporte (RF-13).</summary>
    [MaxLength(20)] public string? NumeroPermisoConducir { get; init; }
}

/// <summary>Respuesta de alta o login: token y datos mínimos de la cuenta.</summary>
public record AuthResponseDto(
    string Token,
    string Email,
    string Role,
    DateTime ExpiresAt);

/// <summary>Datos públicos de una cuenta. Nunca expone el hash ni datos de Identity.</summary>
public record UserDto(
    Guid Id,
    string Email,
    string Nombre,
    string Apellidos,
    string Role);
