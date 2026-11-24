namespace Alquiler_Vehiculos.Validators.Documentos;

/// <summary>
/// Valida el número de pasaporte con el dígito de control de la zona MRZ (RF-18).
/// </summary>
/// <remarks>
/// <para>
/// El algoritmo es el de la zona de lectura mecánica (OACI, Doc 9303). A cada carácter se le
/// asigna un peso cíclico 7, 3, 1; los dígitos valen su propio número, el relleno
/// <c>&lt;</c> vale 0 y las letras valen de 10 (A) a 35 (Z). Se multiplican valores por
/// pesos, se suman y el resto de dividir entre 10 es el dígito de control.
/// </para>
/// <para>
/// Decisión nuestra, pendiente de confirmar con el profesor: el valor guardado en el campo
/// <c>Pasaporte</c> son los nueve caracteres del número de documento seguidos de su dígito
/// de control, es decir, las diez primeras posiciones de la segunda línea de la MRZ.
/// </para>
/// </remarks>
public static class PasaporteValidator {
    private const string Alfabeto = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly int[] Pesos = [7, 3, 1];
    private const int CaracteresNumero = 9;
    private const int LongitudConControl = CaracteresNumero + 1;

    /// <summary>
    /// Indica si el valor tiene nueve caracteres de documento y un dígito de control correcto.
    /// Admite mayúsculas o minúsculas y espacios en los extremos.
    /// </summary>
    /// <param name="pasaporte">Número de pasaporte con su dígito de control, por ejemplo <c>AAA1234565</c>.</param>
    /// <returns>
    /// <c>true</c> si es válido. <c>false</c> si es nulo o vacío, no tiene diez caracteres,
    /// contiene algo distinto de letras, dígitos y el relleno <c>&lt;</c> en el número, el último
    /// carácter no es un dígito, o el dígito de control no corresponde.
    /// </returns>
    public static bool EsValido(string? pasaporte) {
        if (string.IsNullOrWhiteSpace(pasaporte)) return false;

        var texto = pasaporte.Trim().ToUpperInvariant();
        if (texto.Length != LongitudConControl) return false;

        var esperado = CalcularDigitoControl(texto[..CaracteresNumero]);
        return esperado is not null && esperado.ToString() == texto[CaracteresNumero..];
    }

    /// <summary>
    /// Calcula el dígito de control de una cadena según el algoritmo de la MRZ.
    /// </summary>
    /// <param name="campo">Cadena de letras mayúsculas, dígitos y relleno <c>&lt;</c>.</param>
    /// <returns>
    /// El dígito de control (de 0 a 9), o <c>null</c> si la cadena es nula, está vacía
    /// o contiene un carácter no permitido.
    /// </returns>
    public static int? CalcularDigitoControl(string? campo) {
        if (string.IsNullOrEmpty(campo)) return null;

        var suma = 0;
        for (var i = 0; i < campo.Length; i++) {
            var valor = ValorDelCaracter(campo[i]);
            if (valor is null) return null;
            suma += valor.Value * Pesos[i % Pesos.Length];
        }

        return suma % 10;
    }

    private static int? ValorDelCaracter(char c) {
        if (c == '<') return 0;

        for (var i = 0; i < Alfabeto.Length; i++) {
            if (Alfabeto[i] == c) return i;
        }

        return null;
    }
}