using Alquiler_Vehiculos.Dto.Trabajadores;
using Alquiler_Vehiculos.Errors.Common;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Usuarios.Trabajadores;
/// <summary>
/// Contrato del servicio de gestión de trabajadores (RF-12, CU-18).
/// </summary>
public interface ITrabajadorService {
    /// <summary>Devuelve todos los trabajadores activos/no eliminados.</summary>
    Task<IReadOnlyList<TrabajadorResponseDto>> GetAllAsync();
    /// <summary>Busca un trabajador por su identificador.</summary>
    Task<Result<TrabajadorResponseDto, DomainError>> GetByIdAsync(Guid id);
    /// <summary>Da de alta un trabajador junto con su cuenta de usuario y rol Trabajador.</summary>
    Task<Result<TrabajadorResponseDto, DomainError>> CreateAsync(CreateTrabajadorDto dto);
    /// <summary>Modifica los datos de un trabajador existente y sincroniza su email si cambia.</summary>
    Task<Result<TrabajadorResponseDto, DomainError>> UpdateAsync(Guid id, UpdateTrabajadorDto dto);
    /// <summary>Da de baja lógica un trabajador y su cuenta si no es responsable ni tiene reservas vivas.</summary>
    Task<UnitResult<DomainError>> DeleteAsync(Guid id);
    /// <summary>Reactiva un trabajador dado de baja lógica junto con su cuenta.</summary>
    Task<Result<TrabajadorResponseDto, DomainError>> RestoreAsync(Guid id);
}