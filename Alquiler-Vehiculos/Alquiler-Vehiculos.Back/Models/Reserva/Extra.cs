namespace Alquiler_Vehiculos.Models.Reserva;

public class Extra {
    public int Id {get; init;}
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal PrecioDiario {get; set;}
    public bool Activo {get; set;}
}