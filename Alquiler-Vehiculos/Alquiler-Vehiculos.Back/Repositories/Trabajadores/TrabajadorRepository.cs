using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Enums;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using Microsoft.EntityFrameworkCore;

namespace Alquiler_Vehiculos.Repositories.Trabajadores;

public class TrabajadorRepository(AppDbContextUsuarios db, ILogger<TrabajadorRepository> log) : ITrabajadorRepository {
    
    public async Task<IReadOnlyList<Trabajador>> GetAll() {
        return await db.Trabajadores
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Apellidos)
            .ThenBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<Trabajador?> GetById(Guid id) {
        return await db.Trabajadores
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .FirstOrDefaultAsync(c => c.Id == id);
            
    }

    public async Task<Trabajador?> GetByNombre(string nombre) {
        return await db.Trabajadores
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .FirstOrDefaultAsync(c => c.Nombre == nombre);
    }

    public async Task<Trabajador?> GetByEmail(string email) {
        return await db.Trabajadores
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .FirstOrDefaultAsync(c => c.Email == email);
    }

    public async Task<Trabajador> Create(Trabajador trabajador) {
        db.Trabajadores.Add(trabajador);
        await db.SaveChangesAsync();
        log.LogInformation("Trabajador guardadado exitosamente. Trabajador con id: {Id}", trabajador.Id);
        return trabajador;
    }

    public async Task<Trabajador?> Update(Guid id, Trabajador trabajador) {
        var existente = await db.Trabajadores
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        if (existente is null || !existente.Activo) return null;
        existente.Nombre = trabajador.Nombre;
        existente.Apellidos = trabajador.Apellidos;
        existente.Email = trabajador.Email;
        existente.Telefono = trabajador.Telefono;
        existente.FechaNacimiento = trabajador.FechaNacimiento;
        existente.Dni = trabajador.Dni;
        existente.FechaIngreso = trabajador.FechaIngreso;
        existente.Tipo = trabajador.Tipo;
        existente.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        log.LogInformation("Trabajador con id: {Id} actualizado exitosamente", id);
        return existente;
    }

    public async Task<bool> Delete(Guid id) {
        var trabajador = await db.Trabajadores
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted );
        if (trabajador is null) return false;
        var ahora = DateTime.UtcNow;
        trabajador.Activo = false;
        trabajador.IsDeleted = true;
        trabajador.DeletedAt = ahora;
        trabajador.UpdatedAt = ahora;
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.TrabajadorId == id && !u.IsDeleted);
        if (user is not null) {
            user.IsDeleted = true;
            user.DeletedAt = ahora;
            user.UpdatedAt = ahora;
        }
        await db.SaveChangesAsync();
        log.LogInformation("Trabajador dado de baja. Id: {Id}", id);
        return true;
            
    }

    public async Task<Trabajador?> Restore(Guid id) {
        log.LogInformation("Restaurando trabajador con id: {Id}", id);
        var trabajador = await db.Trabajadores
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted);
        if (trabajador is null) return null;
        var ahora = DateTime.UtcNow;
        trabajador.Activo = true;
        trabajador.IsDeleted = false;
        trabajador.DeletedAt = null;
        trabajador.UpdatedAt = ahora;
        var user = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TrabajadorId == id && u.IsDeleted);
        if (user is not null) {
            user.IsDeleted = false;
            user.DeletedAt = null;
            user.UpdatedAt = ahora;
        }
        await db.SaveChangesAsync();
        return trabajador;
    }

    public async Task<bool> EmailEnUso(string email, Guid? excluirId = null) {
        return await db.Trabajadores
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Email == email && (excluirId == null || t.Id != excluirId));
    }

    public async Task<bool> DniEnUso(string dni, Guid? excluirId = null) {
        return await db.Trabajadores
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Dni == dni && (excluirId == null || t.Id != excluirId));
    }

    public async Task<bool> EsResponsableOficinaAsync(Guid trabajadorId) {
        return await db.Oficinas.AsNoTracking().AnyAsync(o => o.ResponsableId == trabajadorId);
    }

    public async Task<bool> TieneReservasVivasAsync(Guid trabajadorId) {
        EstadoReserva[] estadosVivos = [EstadoReserva.Pendiente, EstadoReserva.Confirmada, EstadoReserva.EnCurso];
        return await db.Reservas
            .AsNoTracking()
            .AnyAsync(r => !r.IsDeleted && r.TrabajadorId == trabajadorId && estadosVivos.Contains(r.Estado));
    }
}