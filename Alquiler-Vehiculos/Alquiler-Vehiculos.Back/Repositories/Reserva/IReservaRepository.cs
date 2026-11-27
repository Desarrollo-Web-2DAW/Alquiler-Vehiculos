using System.Data;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Alquiler_Vehiculos.Repositories.Reserva;

public interface IReservaRepository {
    Task<IReadOnlyList<Models.Reserva.Reserva>> FindAllAsync();

    IQueryable<Models.Reserva.Reserva> FindAllAsTraking();

    Task<IEnumerable<Models.Reserva.Reserva>> FindByClienteIdAsync();

    Task<Models.Reserva.Reserva> FindByIdAsync(Guid id);

    Task<Models.Reserva.Reserva> FindByCodigoAsync(string codigo);

    Task<HashSet<string>> FindVehiculosOcupadosAsync(
        DateTime fechaRecogida,
        DateTime fechaDevolucion,
        Guid? excluirReservaId = null);

    Task<bool> VehiculoTieneReservaVivasAsync(string vehiculoId, Guid? excluirRservaId = null!);

    Task<bool> ClienteTieneReservaVivasAsync(Guid clienteId);

    Task<bool> TrabajadorTieneReservasVivasAsync(Guid trabajadorId);

    Task<Models.Reserva.Reserva> FindPendienteCaducadasAsync();

    Task<Result<>> UpdateAsync(Models.Reserva.Reserva reserva);
    
    Task<IDbContextTransaction> BeginTransactionAsyn(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
}