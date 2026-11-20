namespace Alquiler_Vehiculos.Models.Pago;

public class Factura {
    public long Id {get; init;}
    public string Numero {get; set;} = String.Empty;
    public decimal ImporteTotal {get; set;}
    public string RutaDocumento {get; set;} = string.Empty;
    public string DatosFiscales  {get; set;} = string.Empty;

    public Reserva.Reserva Reserva { get; set; } = null!;
    public int ReservaId {get; set;}
    
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
        
}