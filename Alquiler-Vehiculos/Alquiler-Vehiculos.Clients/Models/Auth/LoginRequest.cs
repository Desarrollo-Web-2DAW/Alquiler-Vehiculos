using System.ComponentModel.DataAnnotations;

namespace Alquiler_Vehiculos.Clients.Models.Auth;

/// <summary>
///     Credenciales necesarias para iniciar sesión.
/// </summary>
public class LoginRequest {
    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;

    [Required] public string Password { get; set; } = string.Empty;
}