namespace Alquiler_Vehiculos.Validators.Trabajadores;

public static class TrabajadorValidator {
    private static string _letrasValidas = "TRWAGMYFPDXBNJZSQVHLCKE";
    public static bool ValidarLetraDni(string dni) {
        if (string.IsNullOrWhiteSpace(dni)) return false;
        var limpio = dni.Trim().ToUpperInvariant();
        if (limpio.Length != 9) return false;
        var parteNumero = limpio[..8];
        var letra = limpio[8];
        if (!parteNumero.All(char.IsAsciiDigit) || !char.IsAsciiLetterUpper(letra)) {
            return false;
        }
        var numero = int.Parse(parteNumero);
        return letra == _letrasValidas[numero % 23];
    }
}