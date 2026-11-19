using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Marca;
using Alquiler_Vehiculos.Models.Vehiculo;

namespace Alquiler_Vehiculos.Mappers.Vehiculo;
/// <summary>
/// Clase estática que sirve como base poder crear métodos de extensión para pasar intercambiar datos entre distintas entidades de <see cref="Marca"/>
/// </summary>
public static class MarcaMapper
{
    /// <summary>
    /// Parte de un <see cref="CreateMarcaDto"/> para crear un <see cref="Marca"/> modelo
    /// </summary>
    /// <param name="dto">Data Object Transfer con los datos proporcionados por el cliente</param>
    /// <param name="id">Parámetro opcional para que inicialice en 0 sino se especifica lo contrario</param>
    /// <returns><see cref="Marca"/></returns>
    public static Marca ToMarca(this CreateMarcaDto dto, long id = 0)
    {
        return new Marca(
            Id: id,
            Nombre: dto.Nombre,
            Pais: dto.Pais,
            Web: dto.Web,
            ModelosDisponibles: new List<string>(dto.ModelosDisponibles),
            IsDeleted: false,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: null,
            DeletedAt: null
        );
    }
    
    /// <summary>
    /// Parte de un <see cref="Marca"/> para crear un <see cref="MarcaDto"/>
    /// </summary>
    /// <param name="marca">El modelo</param>
    /// <returns><see cref="MarcaDto"/></returns>
    public static MarcaDto ToDto(this Marca marca)
    {
        return new MarcaDto
        {
            Nombre = marca.Nombre,
            Pais = marca.Pais,
            Web = marca.Web,
            ModelosDisponibles = new List<string>(marca.ModelosDisponibles)
        };
    }
}