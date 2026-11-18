namespace Alquiler_Vehiculos.Models.Vehiculo;

public record Concesionario(long Id, string Nombre, string Ciudad, string Telefono, string Email, bool IsDeleted, DateTime CreatedAt, DateTime? UpdatedAt, DateTime? DeletedAt);