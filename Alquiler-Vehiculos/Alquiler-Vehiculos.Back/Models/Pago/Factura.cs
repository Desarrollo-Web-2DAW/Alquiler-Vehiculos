namespace Alquiler_Vehiculos.Models.Pago;

public class Factura {
    public long Id {get; init;}
    public string Numero {get; set;} = String.Empty;
    public decimal ImporteTotal {get; set;}
    public string RutaDocumento {get; set;}
    public string DatosFiscales  {get; set;}

    public Reservas.Reserva Reserva { get; set; } = null!;
    public int ReservaId {get; set;}
    
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
        
}