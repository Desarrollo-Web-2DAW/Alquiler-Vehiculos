using Alquiler_Vehiculos.Errors.Common;

namespace Alquiler_Vehiculos.Errors.Vehiculo;

public static class VehiculoError
{
    public static ValidationError TipoDeCarroceria()
    {
        return ValidationError.Create("El Acriss dado no es correcto respecto al catálogo, tipo de Categoría no válido");
    }
    
    public static ValidationError TipoCombustible()
    {
        return ValidationError.Create("El Acriss dado no es correcto respecto al catálogo, tipo de Combustible no válido");
    }
    
    public static ValidationError TipoDeTransmision()
    {
        return ValidationError.Create("El Acriss dado no es correcto respecto al catálogo, tipo de Transmision no válido");
    }
}
