using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Concesionario;
using Alquiler_Vehiculos.Models.Vehiculo;

namespace Alquiler_Vehiculos.Mappers.Vehiculo;

/// <summary>
/// Clase estática que sirve como base para poder crear métodos de extensión para intercambiar datos entre distintas entidades de <see cref="Concesionario"/>
/// </summary>
public static class ConcesionarioMapper
{
    /// <summary>
    /// Parte de un <see cref="CreateConcesionarioDto"/> para crear un <see cref="Concesionario"/> modelo
    /// </summary>
    /// <param name="dto">Data Object Transfer con los datos proporcionados por el cliente</param>
    /// <param name="id">Parámetro opcional para que inicialice en 0 si no se especifica lo contrario</param>
    /// <returns><see cref="Concesionario"/></returns>
    public static Concesionario ToConcesionario(this CreateConcesionarioDto dto, long id = 0)
    {
        return new Concesionario(
            Id: id,
            Nombre: dto.Nombre,
            Ciudad: dto.Ciudad,
            Telefono: dto.Telefono,
            Email: dto.Email,
            IsDeleted: false,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: null,
            DeletedAt: null
        );
    }
    
    /// <summary>
    /// Parte de un <see cref="Concesionario"/> para crear un <see cref="ConcesionarioDto"/>
    /// </summary>
    /// <param name="concesionario">El modelo</param>
    /// <returns><see cref="ConcesionarioDto"/></returns>
    public static ConcesionarioDto ToDto(this Concesionario concesionario)
    {
        return new ConcesionarioDto
        {
            Nombre = concesionario.Nombre,
            Ciudad = concesionario.Ciudad,
            Telefono = concesionario.Telefono,
            Email = concesionario.Email
        };
    }
}