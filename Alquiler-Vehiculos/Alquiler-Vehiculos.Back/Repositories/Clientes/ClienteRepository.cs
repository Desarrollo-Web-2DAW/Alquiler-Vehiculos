using Alquiler_Vehiculos.Entities;
using Microsoft.EntityFrameworkCore;

namespace Alquiler_Vehiculos.Repositories.Clientes;

/// <summary>
/// Acceso a datos de clientes (RF-13, RF-14).
/// </summary>
public interface IClienteRepository {
    /// <summary>Indica si alguno de los documentos ya pertenece a un cliente.</summary>
    /// <param name="dni">DNI a comprobar, o null.</param>
    /// <param name="nie">NIE a comprobar, o null.</param>
    /// <param name="pasaporte">Pasaporte a comprobar, o null.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>True si alguno está en uso.</returns>
    Task<bool> DocumentoEnUsoAsync(
        string? dni, string? nie, string? pasaporte, CancellationToken ct = default);
}

/// <summary>
/// Implementación con EF Core sobre PostgreSQL.
/// </summary>
public class ClienteRepository(AppDbContextUsuarios db) : IClienteRepository {
    public async Task<bool> DocumentoEnUsoAsync(
        string? dni, string? nie, string? pasaporte, CancellationToken ct = default) {
        // Tres consultas en lugar de un OR con parámetros nulos:
        // EF traduce esto siempre y el índice único lo hace barato.
        if (!string.IsNullOrWhiteSpace(dni)
            && await db.Clientes.AnyAsync(c => c.Dni == dni, ct)) {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(nie)
            && await db.Clientes.AnyAsync(c => c.Nie == nie, ct)) {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(pasaporte)
            && await db.Clientes.AnyAsync(c => c.Pasaporte == pasaporte, ct)) {
            return true;
        }

        return false;
    }
}
