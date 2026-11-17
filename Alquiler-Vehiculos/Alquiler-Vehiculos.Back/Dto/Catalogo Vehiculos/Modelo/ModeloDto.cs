namespace Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Modelo;

public record ModeloDto {
    public string Nombre { get; set; } = string.Empty;
    public int Anio { get; set; }
    public string NombreMarca { get; set; } = string.Empty;
};