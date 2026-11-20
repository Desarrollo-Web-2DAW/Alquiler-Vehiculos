using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Marca;
using Alquiler_Vehiculos.Models.Vehiculo;

namespace Alquiler_Vehiculos.Mappers.Vehiculo;
/// <summary>
/// Clase estática que sirve como base poder crear métodos de extensión para pasar intercambiar datos entre distintas entidades de <see cref="MarcaModel"/>
/// </summary>
public static class MarcaMapper
{
    /// <summary>
    /// Parte de un <see cref="CreateMarcaDto"/> para crear un <see cref="MarcaModel"/> modelo
    /// </summary>
    /// <param name="dto">Data Object Transfer con los datos proporcionados por el cliente</param>
    /// <param name="id">Parámetro opcional para que inicialice en 0 sino se especifica lo contrario</param>
    /// <returns><see cref="MarcaModel"/></returns>
    public static MarcaModel ToMarca(this CreateMarcaDto dto, long id = 0)
    {
        return new MarcaModel(
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
    /// Parte de un <see cref="MarcaModel"/> para crear un <see cref="MarcaDto"/>
    /// </summary>
    /// <param name="marcaModel">El modelo</param>
    /// <returns><see cref="MarcaDto"/></returns>
    public static MarcaDto ToDto(this MarcaModel marcaModel)
    {
        return new MarcaDto
        {
            Nombre = marcaModel.Nombre,
            Pais = marcaModel.Pais,
            Web = marcaModel.Web,
            ModelosDisponibles = new List<string>(marcaModel.ModelosDisponibles)
        };
    }
}