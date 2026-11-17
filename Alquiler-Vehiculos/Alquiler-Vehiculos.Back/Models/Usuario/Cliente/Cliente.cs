namespace Alquiler_Vehiculos.Models.Cliente;

public class Cliente {
    public Guid Id { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public string Email { get; set; }
    public string Telefono { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public string Dni { get; set; }
    public string Nie { get; set; }
    public string Pasaporte { get; set; }
    public bool IsActivo { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}