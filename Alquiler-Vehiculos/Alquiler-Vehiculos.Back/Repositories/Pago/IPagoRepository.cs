using Alquiler_Vehiculos.Models.Pagos;

namespace Alquiler_Vehiculos.Repositories.Pagos;

/// <summary>
///     Contrato del repositorio de pagos.
/// </summary>
public interface IPagoRepository {
    /// <summary>Obtiene todos los intentos de pago de una reserva.</summary>
    Task<IEnumerable<Pago>> FindByReservaIdAsync(Guid reservaId);

    /// <summary>Obtiene el pago aprobado de una reserva, si existe.</summary>
    Task<Pago?> FindPagoAprobadoAsync(Guid reservaId, bool tracking = false);

    /// <summary>Comprueba si la reserva ya tiene un pago aprobado (CU-20).</summary>
    Task<bool> ExistsPagoAprobadoAsync(Guid reservaId);

    /// <summary>Guarda un intento de pago controlando doble cobro (23505).</summary>
    Task<(bool Guardado, bool ConflictoDobleCobro)> SaveAsync(Pago pago);

    /// <summary>Actualiza un pago existente (por ejemplo, al reembolsarlo).</summary>
    Task UpdateAsync(Pago pago);
}
