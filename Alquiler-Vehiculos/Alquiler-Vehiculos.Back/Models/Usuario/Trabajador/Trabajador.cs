namespace Alquiler_Vehiculos.Models.Trabajador;

public class Trabajador {
    public Guid Id { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public string Email { get; set; }
    public string Telefono { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public string Dni { get; set; }
    
}