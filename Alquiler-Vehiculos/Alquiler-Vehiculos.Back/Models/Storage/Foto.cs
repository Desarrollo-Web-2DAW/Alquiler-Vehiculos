namespace Alquiler_Vehiculos.Models.Storage;

public class Foto
{
    public string Nombre { get; set; } = string.Empty;
    public string Ruta { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long Tamano { get; set; } 
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}