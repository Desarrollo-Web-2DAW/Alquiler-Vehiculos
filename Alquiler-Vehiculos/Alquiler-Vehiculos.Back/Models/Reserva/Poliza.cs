namespace Alquiler_Vehiculos.Models.Reserva;

public class Poliza {
    public int Id {get; init;}
    public string Nombre {get; init;}
    public string Descripcion {get; init;}
    public decimal IncrementoDiario {get; init;}
    public bool Activo {get; init;}
}