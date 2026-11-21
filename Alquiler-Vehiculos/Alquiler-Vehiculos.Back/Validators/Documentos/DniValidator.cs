namespace Alquiler_Vehiculos.Validators.Documentos;

/// <summary>
/// Valida el DNI español: ocho dígitos y una letra de control (RF-16).
/// </summary>
public static class DniValidator {
    private const string LetrasControl = "TRWAGMYFPDXBNJZSQVHLCKE";
    private const int LongitudDni = 9;
    private const int DigitosDni = 8;

    /// <summary>
    /// Indica si el DNI tiene el formato correcto y su letra de control coincide.
    /// </summary>
    /// <param name="dni">DNI a comprobar, por ejemplo <c>12345678Z</c>.</param>
    /// <returns><c>true</c> si es válido; <c>false</c> en caso contrario.</returns>
    public static bool EsValido(string? dni) {
        if (string.IsNullOrWhiteSpace(dni)) return false;

        var texto = dni.Trim().ToUpperInvariant();
        if (texto.Length != LongitudDni) return false;

        var digitos = texto[..DigitosDni];
        if (!digitos.All(char.IsDigit)) return false;
        if (!int.TryParse(digitos, out int numero)) return false;

        return texto[DigitosDni] == CalcularLetra(numero);
    }

    /// <summary>
    /// Calcula la letra de control de un número de DNI o NIE.
    /// </summary>
    /// <param name="numero">Número sin la letra.</param>
    /// <returns>La letra que le corresponde.</returns>
    public static char CalcularLetra(int numero) {
        return LetrasControl[numero % LetrasControl.Length];
    }
}