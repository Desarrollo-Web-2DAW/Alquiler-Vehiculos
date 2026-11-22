using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Concesionario;
using Alquiler_Vehiculos.Services.Common;

namespace Alquiler_Vehiculos.Services.Vehiculo.Concesonario;

/// <summary>
/// Interfaz que define las operaciones del servicio de Concesionario.
/// Hereda las operaciones CRUD.
/// </summary>
public interface IConcesionarioService : ICrudService<CreateConcesionarioDto, UpdateConcesionarioDto, ConcesionarioDto, long>
{
    
}