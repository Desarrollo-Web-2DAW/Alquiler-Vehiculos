using Alquiler_Vehiculos.Clients.Models.Auth;

namespace Alquiler_Vehiculos.Clients.Services.Auth;

/// <summary>
///     Servicio encargado de comunicarse con la API
///     para realizar la autenticación.
/// </summary>
public class AuthApiService(IHttpClientFactory httpClientFactory) {
    /// <summary>
    ///     Envía las credenciales a la API y devuelve el resultado.
    /// </summary>
    public async Task<AuthResponse?> LoginAsync(LoginRequest loginRequest,
        CancellationToken cancellationToken = default) {
        var httpClient = httpClientFactory.CreateClient("AlquilerVehiculosApi");

        var response = await httpClient.PostAsJsonAsync("api/auth/login", loginRequest, cancellationToken);

        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
    }
}