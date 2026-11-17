namespace Alquiler_Vehiculos.Models.Vehiculo.Enums.ACRISS;

/// <summary>
/// Valores posibles para la segunda letra (tipo de carrocería o vehículo) del código ACRISS.
/// </summary>
/// <remarks>
/// Aunque un <c>enum</c> en C# funciona conceptualmente como un mapa clave-valor de constantes enteras
/// para mejorar la legibilidad del código, aquí se sustituye el valor numérico secuencial por defecto (0, 1, 2...)
/// por el código numérico Unicode/ASCII del <c>char</c> correspondiente (ej. <c>Suv = 'F'</c>).
/// De este modo se puede trabajar tanto usando la constante (<c>AcrissTipo.Suv</c>)
/// como casteando directamente desde o hacia el carácter (<c>(char)tipo</c> o <c>(AcrissTipo)'F'</c>).
/// </remarks>
public enum AcrissTipo
{
    DosTresPuertas = 'B',
    DosCuatroPuertas = 'C',
    CuatroCincoPuertas = 'D',
    Familiar = 'W',
    FurgonetaPasajeros = 'V',
    Berlina = 'L',
    Deportivo = 'S',
    Descapotable = 'T',
    Suv = 'F',
    TodoTerrenoAbierto = 'J',
    Especial = 'X',
    PickupCabinaSimple = 'P',
    PickupDobleCabina = 'Q',
    OfertaEspecial = 'Z',
    Coupe = 'E',
    Monovolumen = 'M',
    Recreativo = 'R',
    Autocaravana = 'H',
    DosRuedas = 'Y',
    Roadster = 'N',
    Crossover = 'G',
    FurgonetaComercial = 'K'
}