using System.Data;
using Alquiler_Vehiculos.Models.Reservas;
using Microsoft.EntityFrameworkCore.Storage;

namespace Alquiler_Vehiculos.Repositories.Reservas;

public enum ResultadoGuardadoReserva {
    Ok,
    ConflictoSolape,   // PostgreSQL 23P01: solape de fechas sobre el mismo vehículo (RF-30)
    ConflictoVersion   // EF Core xmin: la reserva fue modificada en paralelo (CU-10 / CU-14)
}

/// <summary>
///     Contrato del repositorio de reservas.
/// </summary>
public interface IReservaRepository {
    /// <summary>Obtiene todas las reservas activas ordenadas por fecha de creación.</summary>
    Task<IEnumerable<Reserva>> FindAllAsync();

    /// <summary>Obtiene reservas como IQueryable sin tracking (útil para filtros y GraphQL).</summary>
    IQueryable<Reserva> FindAllAsNoTracking();

    /// <summary>Obtiene las reservas de un cliente ordenadas por fecha descendente.</summary>
    Task<IEnumerable<Reserva>> FindByClienteIdAsync(Guid clienteId);

    /// <summary>Busca una reserva por ID con líneas, historial y pagos incluidos.</summary>
    Task<Reserva?> FindByIdAsync(Guid id, bool tracking = false);

    /// <summary>Busca una reserva por su código de referencia.</summary>
    Task<Reserva?> FindByCodigoAsync(string codigo);

    /// <summary>Devuelve los IDs de vehículos ocupados en el rango [fechaRecogida, fechaDevolucion).</summary>
    Task<HashSet<string>> FindVehiculosOcupadosAsync(
        DateTime fechaRecogida,
        DateTime fechaDevolucion,
        Guid? excluirReservaId = null);

    /// <summary>Comprueba si un vehículo concreto está libre en el rango solicitado.</summary>
    Task<bool> EstaVehiculoLibreAsync(
        string vehiculoId,
        DateTime fechaRecogida,
        DateTime fechaDevolucion,
        Guid? excluirReservaId = null);

    /// <summary>Comprueba si un vehículo tiene otras reservas vivas (RF-01, RF-07, RF-33).</summary>
    Task<bool> VehiculoTieneReservasVivasAsync(string vehiculoId, Guid? excluirReservaId = null);

    /// <summary>Comprueba si un cliente tiene reservas vivas (RF-13, CU-19).</summary>
    Task<bool> ClienteTieneReservasVivasAsync(Guid clienteId);

    /// <summary>Comprueba si un trabajador tiene reservas vivas (RF-12, CU-18).</summary>
    Task<bool> TrabajadorTieneReservasVivasAsync(Guid trabajadorId);

    /// <summary>Obtiene reservas Pendiente cuyo plazo ExpiraEn ya venció (RF-38).</summary>
    Task<IEnumerable<Reserva>> FindPendientesCaducadasAsync(DateTime ahoraUtc);

    /// <summary>Guarda una reserva nueva controlando conflictos de solape (23P01).</summary>
    Task<ResultadoGuardadoReserva> SaveAsync(Reserva reserva);

    /// <summary>Persiste cambios en una reserva rastreada controlando 23P01 y xmin.</summary>
    Task<ResultadoGuardadoReserva> UpdateAsync(Reserva reserva);

    /// <summary>Inicia una transacción explícita en la base de datos.</summary>
    Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
}