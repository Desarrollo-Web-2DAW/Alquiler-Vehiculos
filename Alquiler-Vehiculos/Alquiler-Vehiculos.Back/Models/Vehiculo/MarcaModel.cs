namespace Alquiler_Vehiculos.Models.Vehiculo;

public record MarcaModel(long Id, string Nombre, string Pais, string Web, ICollection<string> ModelosDisponibles, bool IsDeleted, DateTime CreatedAt, DateTime? UpdatedAt, DateTime? DeletedAt );