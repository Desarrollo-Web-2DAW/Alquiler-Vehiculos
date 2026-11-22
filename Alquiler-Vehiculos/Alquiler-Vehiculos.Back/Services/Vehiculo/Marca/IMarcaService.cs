using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Marca;
using Alquiler_Vehiculos.Services.Common;

namespace Alquiler_Vehiculos.Services.Vehiculo.Marca;

/// <summary>CRUD de marcas y su lista de modelos disponibles (RF-09).</summary>
/// <remarks>
/// Los modelos son nombres incluidos en la marca, no entidades independientes.
/// Update sustituye la lista completa. La baja conserva el documento y no borra vehículos.
/// </remarks>
public interface IMarcaService : ICrudService<CreateMarcaDto, UpdateMarcaDto, MarcaDto, string> {
}
