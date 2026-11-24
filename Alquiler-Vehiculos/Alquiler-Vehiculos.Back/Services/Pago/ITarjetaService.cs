namespace Alquiler_Vehiculos.Services.Pago;


/// <summary>
/// Cifrado simétrico autenticado (AES-256-GCM) y extracción segura de datos de tarjeta (RF-22, RNF-03).
/// </summary>
public interface ITarjetaService {
    string CifrarNumero(string numeroTarjeta);
    string DescifrarNumero(string numeroCifrado);
    string ObtenerUltimosCuatro(string numeroTarjeta);
    string DetectarMarca(string numeroTarjeta);
}