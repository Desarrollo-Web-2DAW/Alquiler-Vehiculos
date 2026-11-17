namespace Alquiler_Vehiculos.Models.Vehiculo.Enums.ACRISS;

/// <summary>
/// Valores posibles para la cuarta letra (combustible y climatización) del código ACRISS de un vehículo.
/// </summary>
/// <remarks>
/// Aunque un <c>enum</c> en C# funciona conceptualmente como un mapa clave-valor de constantes enteras
/// para mejorar la legibilidad del código, aquí se sustituye el valor numérico secuencial por defecto (0, 1, 2...)
/// por el código numérico Unicode/ASCII del <c>char</c> correspondiente (ej. <c>GasolinaConAire = 'V'</c>).
/// De este modo se puede trabajar tanto usando la constante (<c>AcrissCombustibleClimatizacion.GasolinaConAire</c>)
/// como casteando directamente desde o hacia el carácter (<c>(char)combustible</c> o <c>(AcrissCombustibleClimatizacion)'V'</c>).
/// </remarks>
public enum AcrissCombustibleClimatizacion
{
    SinEspecificarConAire = 'R',
    SinEspecificarSinAire = 'N',
    DieselConAire = 'D',
    DieselSinAire = 'Q',
    Hibrido = 'H',
    HibridoEnchufable = 'I',
    ElectricoE = 'E',
    ElectricoC = 'C',
    GlpConAire = 'L',
    GlpSinAire = 'S',
    HidrogenoConAire = 'A',
    HidrogenoSinAire = 'B',
    MulticomustibleConAire = 'M',
    EtanolConAire = 'F',
    GasolinaConAire = 'V',
    GasolinaSinAire = 'Z',
    EtanolSinAire = 'X'
}