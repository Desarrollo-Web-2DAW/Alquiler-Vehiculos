namespace Alquiler_Vehiculos.Enums.Usuarios;

/// <summary>
/// Perfiles de acceso del sistema (RF-46).
/// El público sin identificar no es un rol: es la ausencia de token.
/// El rol efectivo se guarda en la tabla de roles de Identity.
/// </summary>
public enum RolUsuario {
    /// <summary>Cliente que alquila vehículos.</summary>
    Cliente,

    /// <summary>Trabajador de oficina.</summary>
    Trabajador,

    /// <summary>Responsable de la oficina: trabajador con permisos de supervisión.</summary>
    Responsable,

    /// <summary>Administrador del sistema.</summary>
    Admin
}
