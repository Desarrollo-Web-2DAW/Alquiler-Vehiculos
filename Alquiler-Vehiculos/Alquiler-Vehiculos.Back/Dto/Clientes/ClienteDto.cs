namespace Alquiler_Vehiculos.Dto.Clientes;

/// <summary>
/// Datos para dar de alta un cliente de mostrador (RF-13, RF-14).
/// Los tres documentos son opcionales e independientes, pero debe venir al menos uno.
/// </summary>
public record ClienteCreateDto {
    public string Nombre { get; init; } = string.Empty;
    public string Apellidos { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Telefono { get; init; } = string.Empty;
    public DateOnly FechaNacimiento { get; init; }

    public string? Dni { get; init; }
    public string? Nie { get; init; }
    public string? Pasaporte { get; init; }

    /// <summary>Obligatorio solo si el cliente se identifica únicamente con pasaporte (RF-13).</summary>
    public string? NumeroPermisoConducir { get; init; }
}

/// <summary>
/// Datos para modificar un cliente. No incluye el correo: identifica al cliente y no se cambia.
/// </summary>
public record ClienteUpdateDto {
    public string Nombre { get; init; } = string.Empty;
    public string Apellidos { get; init; } = string.Empty;
    public string Telefono { get; init; } = string.Empty;
    public DateOnly FechaNacimiento { get; init; }

    public string? Dni { get; init; }
    public string? Nie { get; init; }
    public string? Pasaporte { get; init; }

    /// <summary>Obligatorio solo si el cliente se identifica únicamente con pasaporte (RF-13).</summary>
    public string? NumeroPermisoConducir { get; init; }
}

/// <summary>Datos de un cliente que devuelve la API.</summary>
public record ClienteResponseDto(
    Guid Id,
    string Nombre,
    string Apellidos,
    string Email,
    string Telefono,
    DateOnly FechaNacimiento,
    string? Dni,
    string? Nie,
    string? Pasaporte,
    string? NumeroPermisoConducir,
    bool Activo,
    bool DadoDeBaja,
    DateTime? FechaBaja,
    DateTime CreatedAt,
    DateTime UpdatedAt);