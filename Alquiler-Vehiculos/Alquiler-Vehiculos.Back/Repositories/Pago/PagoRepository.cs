using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Enums;
using Alquiler_Vehiculos.Models.Pagos;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Alquiler_Vehiculos.Repositories.Pagos;

/// <summary>
///     Implementación del repositorio de pagos.
/// </summary>
public class PagoRepository(
    AppDbContextUsuarios context,
    ILogger<PagoRepository> logger
) : IPagoRepository {
    /// <inheritdoc />
    public async Task<IEnumerable<Pago>> FindByReservaIdAsync(Guid reservaId) {
        logger.LogDebug("Buscando pagos de la reserva {ReservaId}", reservaId);
        return await context.Pagos
            .AsNoTracking()
            .Where(p => p.ReservaId == reservaId)
            .OrderByDescending(p => p.Fecha)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Pago?> FindPagoAprobadoAsync(Guid reservaId, bool tracking = false) {
        logger.LogDebug("Buscando pago aprobado de la reserva {ReservaId}", reservaId);
        var query = tracking ? context.Pagos.AsQueryable() : context.Pagos.AsNoTracking();
        return await query.FirstOrDefaultAsync(
            p => p.ReservaId == reservaId && p.Estado == EstadoPago.Aprobado);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsPagoAprobadoAsync(Guid reservaId) {
        return await context.Pagos
            .AsNoTracking()
            .AnyAsync(p => p.ReservaId == reservaId && p.Estado == EstadoPago.Aprobado);
    }

    /// <inheritdoc />
    public async Task<(bool Guardado, bool ConflictoDobleCobro)> SaveAsync(Pago pago) {
        logger.LogDebug("Guardando intento de pago para reserva {ReservaId}", pago.ReservaId);
        context.Pagos.Add(pago);
        try {
            await context.SaveChangesAsync();
            return (true, false);
        } catch (DbUpdateException ex) when (ex.InnerException is PostgresException {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "UX_pagos_reserva_aprobado"
        }) {
            logger.LogWarning("Doble cobro bloqueado (23505) en reserva {ReservaId}", pago.ReservaId);
            context.ChangeTracker.Clear();
            return (false, true);
        }
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Pago pago) {
        logger.LogDebug("Actualizando pago {PagoId}", pago.Id);
        await context.SaveChangesAsync();
    }
}
