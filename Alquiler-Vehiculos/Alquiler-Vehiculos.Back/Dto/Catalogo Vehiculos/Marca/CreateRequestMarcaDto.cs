namespace Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Marca;

public record CreateRequestMarcaDto {
    public string Nombre { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public string Web { get; set; } = string.Empty;
    public ICollection<string> ModelosDisponibles { get; set; } = [];
};