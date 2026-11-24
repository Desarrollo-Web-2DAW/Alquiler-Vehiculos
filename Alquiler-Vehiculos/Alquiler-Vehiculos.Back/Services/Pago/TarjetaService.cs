using Alquiler_Vehiculos.Validators.Pagos;
using System.Security.Cryptography;
using System.Text;

namespace Alquiler_Vehiculos.Services.Pago;

/// <summary>
/// Implementación AES-256-GCM: la clave vive en configuración (user-secrets / Pagos__EncryptionKey),
/// nunca en el repositorio (RF-22, RNF-03). Usa nonce aleatorio por cifrado.
/// </summary>
public class TarjetaCryptoService : ITarjetaService {
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public TarjetaCryptoService(string claveSecreta) {
        if (string.IsNullOrWhiteSpace(claveSecreta) || Encoding.UTF8.GetByteCount(claveSecreta) < 32) {
            throw new InvalidOperationException(
                "Falta 'Pagos:EncryptionKey' o tiene menos de 32 bytes (256 bits para AES-256-GCM).");
        }

        // Derivamos exactamente 32 bytes fijos con SHA-256 para aceptar cualquier secreto >= 32 bytes.
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(claveSecreta));
    }

    public string CifrarNumero(string numeroTarjeta) {
        var limpio = LuhnValidator.LimpiarNumero(numeroTarjeta);
        var plano = Encoding.UTF8.GetBytes(limpio);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cifrado = new byte[plano.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plano, cifrado, tag);

        // Formato almacenado: [nonce (12B) | tag (16B) | ciphertext] en Base64
        var combinado = new byte[NonceSize + TagSize + cifrado.Length];
        Buffer.BlockCopy(nonce, 0, combinado, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, combinado, NonceSize, TagSize);
        Buffer.BlockCopy(cifrado, 0, combinado, NonceSize + TagSize, cifrado.Length);

        return Convert.ToBase64String(combinado);
    }

    public string DescifrarNumero(string numeroCifrado) {
        var combinado = Convert.FromBase64String(numeroCifrado);
        var nonce = combinado[..NonceSize];
        var tag = combinado[NonceSize..(NonceSize + TagSize)];
        var cifrado = combinado[(NonceSize + TagSize)..];
        var plano = new byte[cifrado.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cifrado, tag, plano);

        return Encoding.UTF8.GetString(plano);
    }

    public string ObtenerUltimosCuatro(string numeroTarjeta) {
        var limpio = LuhnValidator.LimpiarNumero(numeroTarjeta);
        return limpio.Length >= 4 ? limpio[^4..] : limpio;
    }

    public string DetectarMarca(string numeroTarjeta) {
        var limpio = LuhnValidator.LimpiarNumero(numeroTarjeta);
        if (limpio.StartsWith('4')) return "Visa";
        if (limpio.StartsWith("34") || limpio.StartsWith("37")) return "American Express";
        if (limpio.Length >= 2 && int.TryParse(limpio[..2], out var prefijo2) && prefijo2 is >= 51 and <= 55) {
            return "Mastercard";
        }
        if (limpio.Length >= 4 && int.TryParse(limpio[..4], out var prefijo4) && prefijo4 is >= 2221 and <= 2720) {
            return "Mastercard";
        }
        return "Otra";
    }
}