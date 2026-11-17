using Microsoft.AspNetCore.Identity;

namespace Alquiler_Vehiculos.Models.Usuario;

public class User : IdentityUser<Guid> {
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string ClaveHash { get; set; }
    public RolUsuario Rol { get; set; }
    public bool IsActivo { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    
}