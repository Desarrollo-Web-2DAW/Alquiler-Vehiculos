namespace Alquiler_Vehiculos.Clients.Models.Auth;

/// <summary>
///     Respuesta recibida al iniciar sesión correctamente.
/// </summary>
public record AuthResponse(string Token, string Email, string Role, DateTime ExpiresAt);