using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;

using Alquiler_Vehiculos.Services.Common;

namespace Alquiler_Vehiculos.Services.Vehiculo;

public interface IVehiculoService : ICrudService<CreateVehiculoDto, UpdateVehiculoDto, VehiculoDto, string>
{
    
}