namespace Alquiler_Vehiculos.Models.Usuarios.Clientes;

/// <summary>
/// Persona que alquila (RF-13, RF-14).
/// Puede existir sin cuenta de usuario: la oficina da de alta clientes de mostrador.
/// </summary>
public class Cliente {
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;

    public DateOnly FechaNacimiento { get; set; }

    /// <summary>Los tres documentos son opcionales e independientes (RF-14).</summary>
    public string? Dni { get; set; }
    public string? Nie { get; set; }
    public string? Pasaporte { get; set; }

    /// <summary>
    /// Número del permiso de conducir cuando el cliente solo tiene pasaporte (RF-13).
    /// Con DNI o NIE el permiso es el propio documento y este campo va vacío.
    /// </summary>
    public string? NumeroPermisoConducir { get; set; }

    /// <summary>Estado operativo: un cliente inactivo no puede operar, pero conserva sus datos.</summary>
    public bool Activo { get; set; } = true;

    /// <summary>Baja lógica: la fila se conserva con todo su histórico (RF-13).</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Fecha y hora de la baja lógica. Null si el cliente está vivo.</summary>
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Edad cumplida en una fecha concreta. Si cumple años ese día, ya cuenta.</summary>
    public int EdadCumplida(DateOnly fecha) {
        var edad = fecha.Year - FechaNacimiento.Year;
        if (fecha < FechaNacimiento.AddYears(edad)) edad--;
        return edad;
    }

    /// <summary>RF-13: se puede alquilar desde el mismo día en que se cumplen 18.</summary>
    public bool EsMayorDeEdad(DateOnly fecha) => EdadCumplida(fecha) >= 18;

    /// <summary>RF-13: recargo del 25 % si el día de la recogida aún no tiene 25.</summary>
    public bool EsConductorJoven(DateOnly fecha) => EdadCumplida(fecha) < 25;
}
