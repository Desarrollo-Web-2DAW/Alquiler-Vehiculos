namespace Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Concesionario;

public record UpdateRequestConcesionarioDto {
    public string Nombre { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
};