using Alquiler_Vehiculos.Dto.Usuarios;
using Alquiler_Vehiculos.Errors.Common;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Usuarios.Users;

/// <summary>
/// Contrato de administración de cuentas de usuario reservado al rol Admin (RF-46).
/// </summary>
public interface IUserService {
    /// <summary>Lista todas las cuentas del sistema ordenadas por fecha de creación.</summary>
    Task<Result<IReadOnlyList<UserDetalleDto>, DomainError>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Obtiene el detalle administrativo de una cuenta por su identificador.</summary>
    Task<Result<UserDetalleDto, DomainError>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Cambia el rol de una cuenta respetando la compatibilidad persona-rol y el último admin.</summary>
    Task<Result<UserDetalleDto, DomainError>> CambiarRolAsync(Guid id, CambiarRolDto dto, CancellationToken ct = default);

    /// <summary>Da de baja lógica una cuenta impidiendo dejar el sistema sin administradores activos.</summary>
    Task<UnitResult<DomainError>> DesactivarAsync(Guid id, CancellationToken ct = default);

    /// <summary>Reactiva una cuenta dada de baja lógica.</summary>
    Task<Result<UserDetalleDto, DomainError>> ReactivarAsync(Guid id, CancellationToken ct = default);
}
