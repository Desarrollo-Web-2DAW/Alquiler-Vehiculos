using Alquiler_Vehiculos.Enums.Usuarios;

namespace Alquiler_Vehiculos.Dto.Trabajadores;

public class CreateTrabajadorDto {
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public DateOnly FechaNacimiento { get; set; }
    public string Dni { get; set; } = string.Empty;
    public DateOnly FechaIngreso { get; set; }
    public TipoTrabajador Tipo { get; set; }
}