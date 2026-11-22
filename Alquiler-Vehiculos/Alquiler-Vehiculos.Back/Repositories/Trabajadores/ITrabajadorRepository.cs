using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Repositories.Trabajadores;

public interface ITrabajadorRepository {
    Task<IReadOnlyList<Trabajador>> GetAll();
    Task<Trabajador?> GetById(Guid id);
    Task<Trabajador?> GetByNombre(string nombre);
    Task<Trabajador?> GetByEmail(string email);
    Task<Trabajador> Create(Trabajador trabajador);
    Task<Trabajador?> Update(Guid id, Trabajador trabajador);
    Task<bool> Delete(Guid id);
    Task<Trabajador?> Restore(Guid id);
    Task<bool> EmailEnUso(string email, Guid? excluirId = null);
    Task<bool> DniEnUso(string dni,  Guid? excluirId = null);
    Task<bool> EsResponsableOficinaAsync(Guid trabajadorId);
    Task<bool> TieneReservasVivasAsync(Guid trabajadorId);
}