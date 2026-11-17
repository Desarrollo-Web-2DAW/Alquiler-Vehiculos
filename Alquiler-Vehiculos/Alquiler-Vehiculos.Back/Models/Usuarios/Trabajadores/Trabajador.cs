using Alquiler_Vehiculos.Enums.Usuarios;

namespace Alquiler_Vehiculos.Models.Usuarios.Trabajadores;

/// <summary>
/// Persona que trabaja en la oficina (RF-12).
/// Es la persona; su acceso al sistema es una cuenta <c>User</c> con credenciales.
/// </summary>
public class Trabajador {
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public DateOnly FechaNacimiento { get; set; }
    public string Dni { get; set; } = string.Empty;
    public DateOnly FechaIngreso { get; set; }

    /// <summary>Decide qué puede hacer en el taller (RF-08).</summary>
    public TipoTrabajador Tipo { get; set; } = TipoTrabajador.Operaciones;

    /// <summary>
    /// Estado operativo: la baja lo pone a false y le impide iniciar sesión (RF-12),
    /// pero el registro y su histórico se conservan.
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>Baja lógica: la fila se conserva con todo su histórico.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Fecha y hora de la baja lógica. Null si el trabajador está vivo.</summary>
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
