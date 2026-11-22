using Alquiler_Vehiculos.Enums.Taller;
using Alquiler_Vehiculos.Models.Taller;

namespace Alquiler_Vehiculos.Repositories.Taller;

/// <summary>
/// Implementación en memoria de <see cref="IParteTallerRepository"/>.
/// Guarda copias y trabaja bajo cerrojo para que la regla «un solo parte abierto
/// por vehículo» sea atómica, igual que lo será el índice único parcial cuando la
/// implementación de EF Core sustituya a esta.
/// </summary>
public class InMemoryParteTallerRepository : IParteTallerRepository {
    private readonly Dictionary<Guid, ParteTaller> _partes = [];
    private readonly object _cerrojo = new();

    public Task<bool> AgregarAsync(ParteTaller parte, CancellationToken ct = default) {
        ct.ThrowIfCancellationRequested();
        lock (_cerrojo) {
            if (_partes.Values.Any(p =>
                    p.VehiculoId == parte.VehiculoId && p.Estado == EstadoParte.Abierto)) {
                return Task.FromResult(false);
            }
            _partes[parte.Id] = Copia(parte);
            return Task.FromResult(true);
        }
    }

    public Task<ParteTaller?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) {
        ct.ThrowIfCancellationRequested();
        lock (_cerrojo) {
            return Task.FromResult(_partes.TryGetValue(id, out var parte) ? Copia(parte) : null);
        }
    }

    public Task<ParteTaller?> ObtenerAbiertoPorVehiculoAsync(string vehiculoId, CancellationToken ct = default) {
        ct.ThrowIfCancellationRequested();
        lock (_cerrojo) {
            var parte = _partes.Values
                .Where(p => p.VehiculoId == vehiculoId && p.Estado == EstadoParte.Abierto)
                .OrderBy(p => p.Fecha)
                .ThenBy(p => p.Id)
                .FirstOrDefault();
            return Task.FromResult(parte is null ? null : Copia(parte));
        }
    }

    public Task<IReadOnlyList<ParteTaller>> ObtenerAbiertosAsync(CancellationToken ct = default) {
        ct.ThrowIfCancellationRequested();
        lock (_cerrojo) {
            IReadOnlyList<ParteTaller> abiertos = _partes.Values
                .Where(p => p.Estado == EstadoParte.Abierto)
                .OrderBy(p => p.Fecha)
                .ThenBy(p => p.Id)
                .Select(Copia)
                .ToList();
            return Task.FromResult(abiertos);
        }
    }

    public Task<IReadOnlyList<ParteTaller>> ObtenerPorVehiculoAsync(string vehiculoId, CancellationToken ct = default) {
        ct.ThrowIfCancellationRequested();
        lock (_cerrojo) {
            IReadOnlyList<ParteTaller> historial = _partes.Values
                .Where(p => p.VehiculoId == vehiculoId)
                .OrderBy(p => p.Fecha)
                .ThenBy(p => p.Id)
                .Select(Copia)
                .ToList();
            return Task.FromResult(historial);
        }
    }

    public Task ActualizarAsync(ParteTaller parte, CancellationToken ct = default) {
        ct.ThrowIfCancellationRequested();
        lock (_cerrojo) {
            _partes[parte.Id] = Copia(parte);
            return Task.CompletedTask;
        }
    }

    /// <summary>Copia de trabajo: lo que se devuelve o se guarda queda desconectado del resto.</summary>
    private static ParteTaller Copia(ParteTaller parte) {
        return new ParteTaller {
            Id = parte.Id,
            Fecha = parte.Fecha,
            QuienLoTramita = parte.QuienLoTramita,
            Descripcion = parte.Descripcion,
            VehiculoId = parte.VehiculoId,
            Estado = parte.Estado
        };
    }
}
