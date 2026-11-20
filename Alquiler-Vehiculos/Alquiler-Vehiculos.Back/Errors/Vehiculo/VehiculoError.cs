using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
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

    public static ConflictError CambiarDeEstado(string actual, string pedido)
    {
        return new ConflictError(
            $"No se puede cambiar el estado del vehiculo debido a restricciones de negocio: EstadoActual:{actual}-EstadoPedido{pedido}");
    }
}