namespace Alquiler_Vehiculos.Models.Vehiculo.Enums.ACRISS;

/// <summary>
/// Valores posibles para la tercera letra (transmisión y tracción) del código ACRISS de un vehículo.
/// </summary>
/// <remarks>
/// Aunque un <c>enum</c> en C# funciona conceptualmente como un mapa clave-valor de constantes enteras
/// para mejorar la legibilidad del código, aquí se sustituye el valor numérico secuencial por defecto (0, 1, 2...)
/// por el código numérico Unicode/ASCII del <c>char</c> correspondiente (ej. <c>ManualSinTraccionDefinida = 'M'</c>).
/// De este modo se puede trabajar tanto usando la constante (<c>AcrissTransmisionTraccion.ManualSinTraccionDefinida</c>)
/// como casteando directamente desde o hacia el carácter (<c>(char)transmision</c> o <c>(AcrissTransmisionTraccion)'M'</c>).
/// </remarks>
public enum AcrissTransmisionTraccion
{
    ManualSinTraccionDefinida = 'M',
    Manual4Wd = 'N',
    ManualAwd = 'C',
    AutomaticaSinTraccionDefinida = 'A',
    Automatica4Wd = 'B',
    AutomaticaAwd = 'D',
    AutomatizacionNivel3 = 'Q',
    AutomatizacionNivel4 = 'H',
    AutomatizacionNivel5 = 'F'
}
