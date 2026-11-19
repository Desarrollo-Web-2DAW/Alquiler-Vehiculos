using System.ComponentModel.DataAnnotations;

namespace Alquiler_Vehiculos.Dto.Usuario;

public record LoginDto(
    [Required, EmailAddress] string Email,
    [Required] string Password);
    
public record SignupDto(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8), RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$")] string Password,
    [Required, MinLength(2)] string Nombre,
    [Required, MinLength(2)] string Apellidos);

public record AuthResponseDto(string Token, string Email, string Role, DateTime ExpiresAt);


public record UserDto(long id, string Email, string Nombre, string Apellidos, string Role);