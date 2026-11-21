using Alquiler_Vehiculos.Dto.Clientes;
using Alquiler_Vehiculos.Errors.Common;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Usuarios.Clientes;

/// <summary>
/// Reglas de negocio de los clientes (RF-13, RF-14, RF-15).
/// </summary>
public interface IClienteService {
    /// <summary>Lista los clientes con búsqueda, filtro de estado y paginación.</summary>
    /// <param name="filtro">Búsqueda y página. Quien llama decide si se permiten las bajas.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Los clientes de la página, el total de páginas y el total de clientes.</returns>
    Task<(IEnumerable<ClienteResponseDto> Items, int TotalPages, int TotalItems)> GetPagedAsync(
        ClienteFiltroDto filtro, CancellationToken ct = default);

    /// <summary>Busca un cliente por su identificador.</summary>
    /// <param name="id">Identificador del cliente.</param>
    /// <param name="incluirBajas">Si se admite un cliente dado de baja.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El cliente, o un error 404 si no existe.</returns>
    Task<Result<ClienteResponseDto, DomainError>> GetByIdAsync(
        Guid id, bool incluirBajas = false, CancellationToken ct = default);


    /// <summary>Devuelve el cliente al que pertenece una cuenta de usuario.</summary>
    /// <param name="usuarioId">Identificador de la cuenta.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El identificador del cliente, o null si la cuenta no existe o no es de un cliente.</returns>
    Task<Guid?> GetClienteIdDeUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
    /// <summary>Da de alta un cliente de mostrador.</summary>
    /// <param name="dto">Datos del alta.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El cliente creado, o un error 400 de validación o 409 de duplicado.</returns>
    Task<Result<ClienteResponseDto, DomainError>> CreateAsync(
        ClienteCreateDto dto, CancellationToken ct = default);

    /// <summary>Modifica los datos de un cliente. El correo no se cambia.</summary>
    /// <param name="id">Identificador del cliente.</param>
    /// <param name="dto">Datos nuevos.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El cliente modificado, o un error 400, 404 o 409.</returns>
    Task<Result<ClienteResponseDto, DomainError>> UpdateAsync(
        Guid id, ClienteUpdateDto dto, CancellationToken ct = default);

    /// <summary>Da de baja lógica a un cliente: su histórico se conserva.</summary>
    /// <param name="id">Identificador del cliente.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Éxito, o un error 404 si no existe o ya estaba de baja.</returns>
    Task<UnitResult<DomainError>> DeleteAsync(Guid id, CancellationToken ct = default);
}