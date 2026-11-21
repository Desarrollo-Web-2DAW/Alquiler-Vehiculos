using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Models.Usuarios.Clientes;
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

    /// <summary>Devuelve los clientes ordenados por apellidos y nombre.</summary>
    /// <param name="incluirBajas">Si se incluyen los clientes con baja lógica.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La lista de clientes.</returns>
    Task<List<Cliente>> GetAllAsync(bool incluirBajas = false, CancellationToken ct = default);

    /// <summary>Busca un cliente por su identificador.</summary>
    /// <param name="id">Identificador del cliente.</param>
    /// <param name="incluirBajas">Si se admite un cliente con baja lógica.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El cliente, o null si no existe o está de baja y no se admiten bajas.</returns>
    Task<Cliente?> GetByIdAsync(Guid id, bool incluirBajas = false, CancellationToken ct = default);

    /// <summary>Indica si el correo ya pertenece a algún cliente, incluidos los dados de baja.</summary>
    /// <param name="email">Correo ya normalizado.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>True si está en uso.</returns>
    Task<bool> EmailEnUsoAsync(string email, CancellationToken ct = default);

    /// <summary>Indica si alguno de los documentos pertenece a otro cliente distinto del indicado.</summary>
    /// <param name="id">Cliente que se modifica y que se excluye de la comprobación.</param>
    /// <param name="dni">DNI a comprobar, o null.</param>
    /// <param name="nie">NIE a comprobar, o null.</param>
    /// <param name="pasaporte">Pasaporte a comprobar, o null.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>True si alguno está en uso por otro cliente.</returns>
    Task<bool> DocumentoEnUsoPorOtroAsync(
        Guid id, string? dni, string? nie, string? pasaporte, CancellationToken ct = default);

    /// <summary>Guarda un cliente nuevo.</summary>
    /// <param name="cliente">Cliente a guardar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El cliente guardado.</returns>
    Task<Cliente> AddAsync(Cliente cliente, CancellationToken ct = default);

    /// <summary>Copia los datos modificables sobre el cliente existente con el mismo identificador.</summary>
    /// <param name="cliente">Cliente con el identificador y los datos nuevos.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El cliente actualizado, o null si no existe o está de baja.</returns>
    Task<Cliente?> UpdateAsync(Cliente cliente, CancellationToken ct = default);

    /// <summary>Da de baja lógica a un cliente: se conserva la fila y su histórico.</summary>
    /// <param name="id">Identificador del cliente.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>True si se dio de baja; false si no existe o ya estaba de baja.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
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

    public async Task<List<Cliente>> GetAllAsync(bool incluirBajas = false, CancellationToken ct = default) {
        return await db.Clientes
            .AsNoTracking()
            .Where(c => incluirBajas || !c.IsDeleted)
            .OrderBy(c => c.Apellidos)
            .ThenBy(c => c.Nombre)
            .ToListAsync(ct);
    }

    public async Task<Cliente?> GetByIdAsync(Guid id, bool incluirBajas = false, CancellationToken ct = default) {
        return await db.Clientes
            .AsNoTracking()
            .Where(c => c.Id == id && (incluirBajas || !c.IsDeleted))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> EmailEnUsoAsync(string email, CancellationToken ct = default) {
        return await db.Clientes.AnyAsync(c => c.Email == email, ct);
    }

    public async Task<bool> DocumentoEnUsoPorOtroAsync(
        Guid id, string? dni, string? nie, string? pasaporte, CancellationToken ct = default) {
        if (!string.IsNullOrWhiteSpace(dni)
            && await db.Clientes.AnyAsync(c => c.Id != id && c.Dni == dni, ct)) {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(nie)
            && await db.Clientes.AnyAsync(c => c.Id != id && c.Nie == nie, ct)) {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(pasaporte)
            && await db.Clientes.AnyAsync(c => c.Id != id && c.Pasaporte == pasaporte, ct)) {
            return true;
        }

        return false;
    }

    public async Task<Cliente> AddAsync(Cliente cliente, CancellationToken ct = default) {
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync(ct);
        return cliente;
    }

    public async Task<Cliente?> UpdateAsync(Cliente cliente, CancellationToken ct = default) {
        var existente = await db.Clientes
            .Where(c => c.Id == cliente.Id && !c.IsDeleted)
            .FirstOrDefaultAsync(ct);
        if (existente is null) return null;

        existente.Nombre = cliente.Nombre;
        existente.Apellidos = cliente.Apellidos;
        existente.Telefono = cliente.Telefono;
        existente.FechaNacimiento = cliente.FechaNacimiento;
        existente.Dni = cliente.Dni;
        existente.Nie = cliente.Nie;
        existente.Pasaporte = cliente.Pasaporte;
        existente.NumeroPermisoConducir = cliente.NumeroPermisoConducir;
        existente.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return existente;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) {
        var existente = await db.Clientes
            .Where(c => c.Id == id && !c.IsDeleted)
            .FirstOrDefaultAsync(ct);
        if (existente is null) return false;

        existente.IsDeleted = true;
        existente.DeletedAt = DateTime.UtcNow;
        existente.Activo = false;
        existente.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return true;
    }
}