using System.Data;
using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Enums;
using Alquiler_Vehiculos.Models.Reservas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Alquiler_Vehiculos.Repositories.Reservas;

/// <summary>
///     Implementación del repositorio de reservas.
/// </summary>
public class ReservaRepository(
    AppDbContextUsuarios context,
    ILogger<ReservaRepository> logger
) : IReservaRepository {
    private static readonly EstadoReserva[] EstadosVivos = [
        EstadoReserva.Pendiente,
        EstadoReserva.Confirmada,
        EstadoReserva.EnCurso
    ];

    /// <inheritdoc />
    public async Task<IEnumerable<Reserva>> FindAllAsync() {
        logger.LogDebug("Buscando todas las reservas");
        return await QueryCompleta()
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public IQueryable<Reserva> FindAllAsNoTracking() {
        logger.LogDebug("Obteniendo reservas como IQueryable");
        return QueryCompleta()
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Reserva>> FindByClienteIdAsync(Guid clienteId) {
        logger.LogDebug("Buscando reservas del cliente {ClienteId}", clienteId);
        return await QueryCompleta()
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.ClienteId == clienteId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Reserva?> FindByIdAsync(Guid id, bool tracking = false) {
        logger.LogDebug("Buscando reserva por ID {ReservaId}", id);
        var query = tracking ? QueryCompleta() : QueryCompleta().AsNoTracking();
        return await query.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
    }

    /// <inheritdoc />
    public async Task<Reserva?> FindByCodigoAsync(string codigo) {
        logger.LogDebug("Buscando reserva por código {Codigo}", codigo);
        return await QueryCompleta()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Codigo == codigo && !r.IsDeleted);
    }

    /// <inheritdoc />
    public async Task<HashSet<string>> FindVehiculosOcupadosAsync(
        DateTime fechaRecogida,
        DateTime fechaDevolucion,
        Guid? excluirReservaId = null) {
        logger.LogDebug("Consultando vehículos ocupados entre {Inicio} y {Fin}", fechaRecogida, fechaDevolucion);

        var ids = await context.Reservas
            .AsNoTracking()
            .Where(r => !r.IsDeleted
                && EstadosVivos.Contains(r.Estado)
                && (excluirReservaId == null || r.Id != excluirReservaId)
                && r.FechaRecogida < fechaDevolucion
                && r.FechaDevolucion > fechaRecogida)
            .Select(r => r.VehiculoId)
            .Distinct()
            .ToListAsync();

        return ids.ToHashSet(StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public async Task<bool> EstaVehiculoLibreAsync(
        string vehiculoId,
        DateTime fechaRecogida,
        DateTime fechaDevolucion,
        Guid? excluirReservaId = null) {
        var haySolape = await context.Reservas
            .AsNoTracking()
            .AnyAsync(r => !r.IsDeleted
                && r.VehiculoId == vehiculoId
                && EstadosVivos.Contains(r.Estado)
                && (excluirReservaId == null || r.Id != excluirReservaId)
                && r.FechaRecogida < fechaDevolucion
                && r.FechaDevolucion > fechaRecogida);

        return !haySolape;
    }

    /// <inheritdoc />
    public async Task<bool> VehiculoTieneReservasVivasAsync(string vehiculoId, Guid? excluirReservaId = null) {
        return await context.Reservas
            .AsNoTracking()
            .AnyAsync(r => !r.IsDeleted
                && r.VehiculoId == vehiculoId
                && EstadosVivos.Contains(r.Estado)
                && (excluirReservaId == null || r.Id != excluirReservaId));
    }

    /// <inheritdoc />
    public async Task<bool> ClienteTieneReservasVivasAsync(Guid clienteId) {
        return await context.Reservas
            .AsNoTracking()
            .AnyAsync(r => !r.IsDeleted
                && r.ClienteId == clienteId
                && EstadosVivos.Contains(r.Estado));
    }

    /// <inheritdoc />
    public async Task<bool> TrabajadorTieneReservasVivasAsync(Guid trabajadorId) {
        return await context.Reservas
            .AsNoTracking()
            .AnyAsync(r => !r.IsDeleted
                && r.TrabajadorId == trabajadorId
                && EstadosVivos.Contains(r.Estado));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Reserva>> FindPendientesCaducadasAsync(DateTime ahoraUtc) {
        logger.LogDebug("Buscando reservas pendientes caducadas hasta {AhoraUtc}", ahoraUtc);
        return await context.Reservas
            .Include(r => r.Cliente)
            .Include(r => r.HistorialEstados)
            .Where(r => !r.IsDeleted
                && r.Estado == EstadoReserva.Pendiente
                && r.ExpiraEn != null
                && r.ExpiraEn <= ahoraUtc)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ResultadoGuardadoReserva> SaveAsync(Reserva reserva) {
        logger.LogDebug("Guardando nueva reserva {Codigo} para vehículo {VehiculoId}", reserva.Codigo, reserva.VehiculoId);
        context.Reservas.Add(reserva);
        return await GuardarControlandoConcurrenciaAsync();
    }

    /// <inheritdoc />
    public async Task<ResultadoGuardadoReserva> UpdateAsync(Reserva reserva) {
        logger.LogDebug("Actualizando reserva {ReservaId}", reserva.Id);
        reserva.UpdatedAt = DateTime.UtcNow;
        return await GuardarControlandoConcurrenciaAsync();
    }

    /// <inheritdoc />
    public async Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) {
        return await context.Database.BeginTransactionAsync(isolationLevel);
    }

    private async Task<ResultadoGuardadoReserva> GuardarControlandoConcurrenciaAsync() {
        try {
            await context.SaveChangesAsync();
            return ResultadoGuardadoReserva.Ok;
        } catch (DbUpdateConcurrencyException) {
            logger.LogWarning("Conflicto optimista (xmin) al actualizar una reserva modificada en paralelo");
            context.ChangeTracker.Clear();
            return ResultadoGuardadoReserva.ConflictoVersion;
        } catch (DbUpdateException ex) when (ex.InnerException is PostgresException {
            SqlState: PostgresErrorCodes.ExclusionViolation
        }) {
            logger.LogWarning("Conflicto de concurrencia (23P01) en PostgreSQL por solape de fechas");
            context.ChangeTracker.Clear();
            return ResultadoGuardadoReserva.ConflictoSolape;
        }
    }

    private IQueryable<Reserva> QueryCompleta() =>
        context.Reservas
            .Include(r => r.Cliente)
            .Include(r => r.Trabajador)
            .Include(r => r.Lineas)
            .Include(r => r.HistorialEstados.OrderBy(h => h.FechaCambio))
            .Include(r => r.Pagos)
            .AsSplitQuery();
}
