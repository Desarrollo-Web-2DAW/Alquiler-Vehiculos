namespace Alquiler_Vehiculos.Validators.Documentos;

/// <summary>
/// Valida el NIE español: letra inicial X, Y o Z, siete dígitos y una letra de control (RF-16).
/// </summary>
public static class NieValidator {
    private const string LetrasInicio = "XYZ";
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

        var prefijo = LetrasInicio.IndexOf(texto[0]);
        if (prefijo < 0) return false;

        var numero = prefijo;
        for (var i = 1; i <= DigitosNie; i++) {
            if (texto[i] is < '0' or > '9') return false;
            numero = numero * 10 + (texto[i] - '0');
        }

        return texto[LongitudNie - 1] == DniValidator.CalcularLetra(numero);
    }
}