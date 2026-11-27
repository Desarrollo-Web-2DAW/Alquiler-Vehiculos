namespace Alquiler_Vehiculos.Enums;

public enum ResultadoGuardadoReserva {
    Ok,
    ConflictoSolape,   // PostgreSQL 23P01: solape de fechas sobre el mismo vehículo (RF-30)
    ConflictoVersion   // EF Core xmin: la reserva fue modificada en paralelo (CU-10 / CU-14)
}
