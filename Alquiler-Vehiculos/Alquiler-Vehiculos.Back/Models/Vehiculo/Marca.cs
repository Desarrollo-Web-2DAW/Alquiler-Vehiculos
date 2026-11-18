namespace Alquiler_Vehiculos.Models.Vehiculo;

public record Marca(long Id, string Nombre, string Pais, string Web, bool IsDeleted, DateTime CreatedAt, DateTime? UpdatedAt, DateTime? DeletedAt );