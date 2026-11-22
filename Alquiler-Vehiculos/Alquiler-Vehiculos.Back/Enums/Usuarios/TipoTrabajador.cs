namespace Alquiler_Vehiculos.Enums.Usuarios;

/// <summary>
/// Tipo de trabajador (RF-12). Solo el tipo <see cref="Mecanico"/> abre y cierra partes (RF-08).
/// El responsable no es un tipo: es el trabajador designado en Oficina.Responsable (RF-11).
/// </summary>
public enum TipoTrabajador {
    Operaciones,
    Mecanico
}
