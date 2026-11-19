namespace Alquiler_Vehiculos.Models.Reserva;

public class Extra {
    public int Id {get; init;}
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public decimal PrecioDiaria {get; init;}
    public bool Activo {get; init;}
}