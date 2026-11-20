using Alquiler_Vehiculos.Errors.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Alquiler_Vehiculos.Errors.Vehiculo;

public static class VehiculoError
{
    public static BadRequestError TipoDeCarroceria()
    {
        return new BadRequestError("El Acriss dado no es correcto respecto al catálogo, tipo de Categoría no válido");
    }
    
    public static BadRequestError TipoCombustible()
    {
        return new BadRequestError("El Acriss dado no es correcto respecto al catálogo, tipo de Combustible no válido");
    }
    
    public static BadRequestError TipoDeTransmision()
    {
        return new BadRequestError("El Acriss dado no es correcto respecto al catálogo, tipo de Transmision no válido");
    }
}