using Alquiler_Vehiculos.Dto.Trabajadores;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;

namespace Alquiler_Vehiculos.Mappers.Trabajadores;

/// <summary>
/// Conversiones entre entidades <see cref="Trabajador"/> y sus DTOs.
/// </summary>
public static class TrabajadorMapper {
    /// <summary>Convierte un DTO de alta en entidad <see cref="Trabajador"/> normalizada.</summary>
    public static Trabajador ToEntity(this CreateTrabajadorDto dto) {
        return new Trabajador {
            Nombre = dto.Nombre.Trim(),
            Apellidos = dto.Apellidos.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Telefono = dto.Telefono.Trim(),
            FechaNacimiento = dto.FechaNacimiento,
            Dni = dto.Dni.Trim().ToUpperInvariant(),
            FechaIngreso = dto.FechaIngreso,
            Tipo = dto.Tipo
        };
    }

    /// <summary>Convierte un DTO de modificación en entidad <see cref="Trabajador"/> normalizada.</summary>
    public static Trabajador ToEntity(this UpdateTrabajadorDto dto) {
        return new Trabajador {
            Nombre = dto.Nombre.Trim(),
            Apellidos = dto.Apellidos.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Telefono = dto.Telefono.Trim(),
            FechaNacimiento = dto.FechaNacimiento,
            Dni = dto.Dni.Trim().ToUpperInvariant(),
            FechaIngreso = dto.FechaIngreso,
            Tipo = dto.Tipo
        };
    }

    /// <summary>Convierte una entidad <see cref="Trabajador"/> en su DTO de respuesta.</summary>
    public static TrabajadorResponseDto ToDto(this Trabajador trabajador) {
        return new TrabajadorResponseDto {
            Id = trabajador.Id,
            Nombre = trabajador.Nombre,
            Apellidos = trabajador.Apellidos,
            Email = trabajador.Email,
            Telefono = trabajador.Telefono,
            FechaNacimiento = trabajador.FechaNacimiento,
            Dni = trabajador.Dni,
            FechaIngreso = trabajador.FechaIngreso,
            Tipo = trabajador.Tipo,
            Activo = trabajador.Activo
        };
    }

    /// <summary>Convierte una colección de trabajadores en una lista de DTOs de respuesta.</summary>
    public static IReadOnlyList<TrabajadorResponseDto> ToResponseDtoList(this IEnumerable<Trabajador> trabajadores) {
        return trabajadores.Select(t => t.ToDto()).ToList();
    }
}