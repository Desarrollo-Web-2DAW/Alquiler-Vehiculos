namespace Alquiler_Vehiculos.Validators.Documentos;

/// <summary>
/// Valida el NIE español: letra inicial X, Y o Z, siete dígitos y una letra de control (RF-16).
/// </summary>
public static class NieValidator {
    private const int LongitudNie = 9;
    private const int DigitosNie = 7;

    /// <summary>
    /// Indica si el NIE tiene el formato correcto y su letra de control coincide.
    /// </summary>
    /// <param name="nie">NIE a comprobar, por ejemplo <c>X1234567L</c>.</param>
    /// <returns><c>true</c> si es válido; <c>false</c> en caso contrario.</returns>
    public static bool EsValido(string? nie) {
        if (string.IsNullOrWhiteSpace(nie)) return false;

        var texto = nie.Trim().ToUpperInvariant();
        if (texto.Length != LongitudNie) return false;

        string prefijo;
        if (texto[0] == 'X') prefijo = "0";
        else if (texto[0] == 'Y') prefijo = "1";
        else if (texto[0] == 'Z') prefijo = "2";
        else return false;

        var digitos = texto.Substring(1, DigitosNie);
        if (!digitos.All(char.IsDigit)) return false;
        if (!int.TryParse(prefijo + digitos, out int numero)) return false;

        return texto[LongitudNie - 1] == DniValidator.CalcularLetra(numero);
    }
}