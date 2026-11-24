using Alquiler_Vehiculos.Dto.Clientes;
using Alquiler_Vehiculos.Models.Usuarios.Clientes;

namespace Alquiler_Vehiculos.Mappers;

/// <summary>
/// Conversiones entre el modelo <see cref="Cliente"/> y sus DTOs.
/// </summary>
/// <remarks>
/// Al convertir un DTO de entrada se normalizan los datos: nombres y teléfono sin espacios
/// sobrantes, correo en minúsculas, documentos en mayúsculas y los documentos en blanco
/// como nulos. Así las comparaciones de duplicados y los índices únicos trabajan
/// siempre con el mismo formato.
/// </remarks>
public static class ClienteMapper {
    /// <summary>Convierte un cliente en el DTO que devuelve la API.</summary>
    /// <param name="cliente">Cliente a convertir.</param>
    /// <returns>El DTO de respuesta.</returns>
    public static ClienteResponseDto ToDto(this Cliente cliente) =>
        new(cliente.Id, cliente.Nombre, cliente.Apellidos, cliente.Email, cliente.Telefono,
            cliente.FechaNacimiento, cliente.Dni, cliente.Nie, cliente.Pasaporte,
            cliente.NumeroPermisoConducir, cliente.Activo, cliente.IsDeleted, cliente.DeletedAt,
            cliente.CreatedAt, cliente.UpdatedAt);

    /// <summary>Convierte los datos del alta en un cliente nuevo.</summary>
    /// <param name="dto">Datos del alta.</param>
    /// <returns>Un cliente con los datos normalizados.</returns>
    public static Cliente ToModel(this ClienteCreateDto dto) => new() {
        Nombre = dto.Nombre.Trim(),
        Apellidos = dto.Apellidos.Trim(),
        Email = dto.Email.Trim().ToLowerInvariant(),
        Telefono = dto.Telefono.Trim(),
        FechaNacimiento = dto.FechaNacimiento,
        Dni = NormalizarDocumento(dto.Dni),
        Nie = NormalizarDocumento(dto.Nie),
        Pasaporte = NormalizarDocumento(dto.Pasaporte),
        NumeroPermisoConducir = NormalizarDocumento(dto.NumeroPermisoConducir)
    };

    /// <summary>Convierte los datos de la modificación en un cliente con los datos nuevos.</summary>
    /// <param name="dto">Datos de la modificación.</param>
    /// <returns>
    /// Un cliente con los datos normalizados y sin correo: el repositorio copia solo los campos
    /// que cambian sobre el cliente existente.
    /// </returns>
    public static Cliente ToModel(this ClienteUpdateDto dto) => new() {
        Nombre = dto.Nombre.Trim(),
        Apellidos = dto.Apellidos.Trim(),
        Telefono = dto.Telefono.Trim(),
        FechaNacimiento = dto.FechaNacimiento,
        Dni = NormalizarDocumento(dto.Dni),
        Nie = NormalizarDocumento(dto.Nie),
        Pasaporte = NormalizarDocumento(dto.Pasaporte),
        NumeroPermisoConducir = NormalizarDocumento(dto.NumeroPermisoConducir)
    };

    private static string? NormalizarDocumento(string? valor) {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        return valor.Trim().ToUpperInvariant();
    }
}