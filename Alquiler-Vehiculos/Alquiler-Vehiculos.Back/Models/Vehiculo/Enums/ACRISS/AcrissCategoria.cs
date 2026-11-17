namespace Alquiler_Vehiculos.Models.Vehiculo.Enums.ACRISS;

/// <summary>
/// Valores posibles para la primera letra (categoría) del código ACRISS de un vehículo.
/// </summary>
/// <remarks>
/// Aunque un <c>enum</c> en C# funciona conceptualmente como un mapa clave-valor de constantes enteras
/// para mejorar la legibilidad del código, aquí se sustituye el valor numérico secuencial por defecto (0, 1, 2...)
/// por el código numérico Unicode/ASCII del <c>char</c> correspondiente (ej. <c>Mini = 'M'</c>).
/// De este modo se puede trabajar tanto usando la constante (<c>AcrissCategoria.Mini</c>)
/// como casteando directamente desde o hacia el carácter (<c>(char)categoria</c> o <c>(AcrissCategoria)'M'</c>).
/// </remarks>
public enum AcrissCategoria
{
    Mini = 'M',
    MiniElite = 'N',
    Economico = 'E',
    EconomicoElite = 'H',
    Compacto = 'C',
    CompactoElite = 'D',
    Intermedio = 'I',
    IntermedioElite = 'J',
    Estandar = 'S',
    EstandarElite = 'R',
    Grande = 'F',
    GrandeElite = 'G',
    Premium = 'P',
    PremiumElite = 'U',
    Lujo = 'L',
    LujoElite = 'W',
    Sobredimensionado = 'O',
    Especial = 'X'
}