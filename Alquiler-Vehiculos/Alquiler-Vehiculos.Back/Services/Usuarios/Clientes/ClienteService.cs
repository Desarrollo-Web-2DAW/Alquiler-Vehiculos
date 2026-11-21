using Alquiler_Vehiculos.Dto.Clientes;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Usuarios;
using Alquiler_Vehiculos.Mappers;
using Alquiler_Vehiculos.Repositories.Clientes;
using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;

namespace Alquiler_Vehiculos.Services.Usuarios.Clientes;

/// <summary>
/// Reglas de negocio de los clientes (RF-13, RF-14, RF-15).
/// No conoce los roles: quién puede llamar a cada operación lo decide el controlador.
/// </summary>
/// <remarks>
/// Pendiente (RF-13): la baja debe rechazarse con 409 si el cliente tiene reservas vivas
/// (<see cref="ClienteError.ConReservasVivas"/>). Depende del módulo de reservas, que todavía
/// no existe, y se añadirá cuando esté.
/// </remarks>
public class ClienteService(
    IClienteRepository repositorio,
    IValidator<ClienteCreateDto> validadorAlta,
    IValidator<ClienteUpdateDto> validadorModificacion) : IClienteService {
    /// <inheritdoc />
    public async Task<(IEnumerable<ClienteResponseDto> Items, int TotalPages, int TotalItems)> GetPagedAsync(
        ClienteFiltroDto filtro, CancellationToken ct = default) {
        var page = Math.Max(filtro.Page, 1);
        var pageSize = Math.Clamp(filtro.PageSize, 1, 100);

        var clientes = await repositorio.GetAllAsync(filtro.IncluirBajas, ct);
        var filtrados = clientes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filtro.Texto)) {
            var texto = filtro.Texto.Trim();
            filtrados = filtrados.Where(c =>
                c.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || c.Apellidos.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || c.Email.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || (c.Dni is not null && c.Dni.Contains(texto, StringComparison.OrdinalIgnoreCase))
                || (c.Nie is not null && c.Nie.Contains(texto, StringComparison.OrdinalIgnoreCase))
                || (c.Pasaporte is not null && c.Pasaporte.Contains(texto, StringComparison.OrdinalIgnoreCase)));
        }

        if (filtro.Activo.HasValue) {
            filtrados = filtrados.Where(c => c.Activo == filtro.Activo.Value);
        }

        var lista = filtrados.ToList();
        var totalItems = lista.Count;
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        var items = lista.Skip((page - 1) * pageSize).Take(pageSize).Select(c => c.ToDto());

        return (items, totalPages, totalItems);
    }

    /// <inheritdoc />
    public async Task<Result<ClienteResponseDto, DomainError>> GetByIdAsync(
        Guid id, bool incluirBajas = false, CancellationToken ct = default) {
        var cliente = await repositorio.GetByIdAsync(id, incluirBajas, ct);
        if (cliente is null) {
            return Result.Failure<ClienteResponseDto, DomainError>(ClienteError.NotFound(id));
        }

        return Result.Success<ClienteResponseDto, DomainError>(cliente.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<ClienteResponseDto, DomainError>> CreateAsync(
        ClienteCreateDto dto, CancellationToken ct = default) {
        var validacion = await validadorAlta.ValidateAsync(dto);
        if (!validacion.IsValid) {
            return Result.Failure<ClienteResponseDto, DomainError>(ErroresDeValidacion(validacion));
        }

        var cliente = dto.ToModel();

        if (await repositorio.EmailEnUsoAsync(cliente.Email, ct)) {
            return Result.Failure<ClienteResponseDto, DomainError>(ClienteError.EmailExistente(cliente.Email));
        }

        if (await repositorio.DocumentoEnUsoAsync(cliente.Dni, cliente.Nie, cliente.Pasaporte, ct)) {
            var documento = cliente.Dni ?? cliente.Nie ?? cliente.Pasaporte ?? string.Empty;
            return Result.Failure<ClienteResponseDto, DomainError>(ClienteError.DocumentoExistente(documento));
        }

        var creado = await repositorio.AddAsync(cliente, ct);

        return Result.Success<ClienteResponseDto, DomainError>(creado.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<ClienteResponseDto, DomainError>> UpdateAsync(
        Guid id, ClienteUpdateDto dto, CancellationToken ct = default) {
        var validacion = await validadorModificacion.ValidateAsync(dto);
        if (!validacion.IsValid) {
            return Result.Failure<ClienteResponseDto, DomainError>(ErroresDeValidacion(validacion));
        }

        var existente = await repositorio.GetByIdAsync(id, false, ct);
        if (existente is null) {
            return Result.Failure<ClienteResponseDto, DomainError>(ClienteError.NotFound(id));
        }

        var datos = dto.ToModel();
        datos.Id = id;

        if (await repositorio.DocumentoEnUsoPorOtroAsync(id, datos.Dni, datos.Nie, datos.Pasaporte, ct)) {
            var documento = datos.Dni ?? datos.Nie ?? datos.Pasaporte ?? string.Empty;
            return Result.Failure<ClienteResponseDto, DomainError>(ClienteError.DocumentoExistente(documento));
        }

        var actualizado = await repositorio.UpdateAsync(datos, ct);
        if (actualizado is null) {
            return Result.Failure<ClienteResponseDto, DomainError>(ClienteError.NotFound(id));
        }

        return Result.Success<ClienteResponseDto, DomainError>(actualizado.ToDto());
    }

    /// <inheritdoc />
    public async Task<UnitResult<DomainError>> DeleteAsync(Guid id, CancellationToken ct = default) {
        var borrado = await repositorio.DeleteAsync(id, ct);
        if (!borrado) {
            return UnitResult.Failure<DomainError>(ClienteError.NotFound(id));
        }

        return UnitResult.Success<DomainError>();
    }

    private static ValidationError ErroresDeValidacion(ValidationResult resultado) {
        var errores = resultado.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return ValidationError.WithFieldErrors(errores);
    }
}