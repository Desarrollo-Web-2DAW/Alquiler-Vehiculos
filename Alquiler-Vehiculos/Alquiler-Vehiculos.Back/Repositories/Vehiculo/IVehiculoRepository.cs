using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Repositories.Common;

namespace Alquiler_Vehiculos.Repositories.Vehiculo;

public interface IVehiculoRepository : ICrud<string, VehiculoModel>
{
    
}