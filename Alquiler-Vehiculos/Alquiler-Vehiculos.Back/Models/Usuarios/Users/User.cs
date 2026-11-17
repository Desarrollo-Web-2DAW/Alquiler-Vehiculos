using Alquiler_Vehiculos.Models.Usuarios.Clientes;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using Microsoft.AspNetCore.Identity;

namespace Alquiler_Vehiculos.Models.Usuarios;

/// <summary>
/// Credenciales de acceso de una persona (RI-02).
/// La persona física vive en <c>Cliente</c> o <c>Trabajador</c>.
/// El rol no se guarda aquí: vive en la tabla de roles de Identity.
/// La baja lógica conserva la fila y todos sus datos.
/// </summary>
public class User : IdentityUser<Guid> {
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Baja lógica de la cuenta: la fila se conserva con todos sus datos.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Fecha y hora de la baja lógica. Null si la cuenta está viva.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Cuenta de un cliente. Null si la cuenta pertenece a un trabajador.</summary>
    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    /// <summary>Cuenta de un trabajador. Null si la cuenta pertenece a un cliente.</summary>
    public Guid? TrabajadorId { get; set; }
    public Trabajador? Trabajador { get; set; }
}
