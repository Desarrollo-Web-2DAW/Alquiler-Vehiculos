using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Query;
using Alquiler_Vehiculos.Errors.Common;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;

/// <summary>
/// Define las operaciones de persistencia y consulta de vehículos.
/// No hereda de ICrud porque Create devuelve Result con el vehículo o un error de dominio,
/// mientras que el contrato genérico devuelve únicamente la entidad creada.
/// Este contrato permite comunicar el conflicto de matrícula duplicada sin modificar
/// el CRUD compartido por los demás repositorios.
/// </summary>
public interface IVehiculoRepository {
    
    /// <summary>
    /// Crea un vehículo en el repositorio.
    /// </summary>
    /// <param name="item">Vehículo que se desea guardar.</param>
    /// <returns>El vehículo creado o un error de dominio si su matrícula ya está registrada.</returns>
    Task<Result<VehiculoModel, DomainError>> Create(VehiculoModel item);

    /// <summary>
    /// Obtiene un vehículo por su identificador, incluidos los dados de baja.
    /// </summary>
    /// <param name="key">Identificador del vehículo.</param>
    /// <returns>El vehículo encontrado o null si no existe.</returns>
    Task<VehiculoModel?> GetById(string key);

    /// <summary>
    /// Realiza la baja lógica de un vehículo, dejándolo Retirado y con Activo en false.
    /// </summary>
    /// <param name="key">Identificador del vehículo que se desea desactivar.</param>
    /// <returns>True si se realiza la baja o false si el vehículo no existe.</returns>
    /// <remarks>Las comprobaciones de estado, reservas y partes corresponden a la lógica de negocio.</remarks>
    Task<bool> SoftDelete(string key);

    /// <summary>
    /// Actualiza los datos de un vehículo activo.
    /// </summary>
    /// <param name="key">Identificador del vehículo que se desea modificar.</param>
    /// <param name="item">Datos que se aplicarán al vehículo existente.</param>
    /// <returns>El vehículo actualizado o null si no existe o está inactivo.</returns>
    Task<VehiculoModel?> Update(string key, VehiculoModel item);

    /// <summary>
    /// Obtiene todos los vehículos activos del repositorio.
    /// </summary>
    /// <returns>Colección de vehículos activos ordenados por matrícula.</returns>
    Task<IEnumerable<VehiculoModel>> GetAll();

    /// <summary>
    /// Consulta los vehículos que cumplen los filtros indicados.
    /// </summary>
    /// <param name="request">Filtros opcionales de la búsqueda de vehículos.</param>
    /// <returns>Vehículos coincidentes, o una colección vacía si no hay coincidencias.</returns>
    /// <remarks>
    /// Aplica los filtros del catálogo, no las fechas de recogida y devolución.
    /// El servicio combina estos vehículos con la consulta de disponibilidad por fechas
    /// y calcula las unidades totales y libres de cada grupo.
    /// </remarks>
    Task<IEnumerable<VehiculoModel>> GetByFilters(QueryRequestVehiculoDto request);

    /// <summary>
    /// Comprueba si existe un vehículo por su identificador, incluidos los dados de baja.
    /// </summary>
    /// <param name="key">Identificador del vehículo.</param>
    /// <returns>True si existe o false si no existe.</returns>
    Task<bool> ExistById(string key);
    
    /// <summary>
    /// Comprueba si una matrícula está registrada, incluidos los vehículos dados de baja.
    /// </summary>
    /// <param name="matricula">Matrícula que se desea comprobar.</param>
    /// <returns>True si la matrícula está registrada o false si no lo está.</returns>
    Task<bool> ExistByMatricula(string matricula);
    
    /// <summary>
    /// Obtiene un vehículo por su matrícula, incluidos los dados de baja.
    /// </summary>
    /// <param name="matricula">Matrícula del vehículo que se desea consultar.</param>
    /// <returns>El vehículo encontrado o null si no existe.</returns>
    Task<VehiculoModel?> GetByMatricula(string matricula);
    
    /// <summary>
    /// Cuenta los vehículos activos y recupera la página solicitada, ordenada por matrícula.
    /// </summary>
    /// <param name="page">Número de página, desde 1.</param>
    /// <param name="pageSize">Número máximo de elementos por página.</param>
    /// <returns>Elementos de la página y total de registros.</returns>
    Task<(IEnumerable<VehiculoModel> Items, int TotalItems)> GetPaged(int page, int pageSize);
    
    /// <summary>
    /// Reactiva un vehículo por su identificador, dejándolo Disponible y con Activo en true.
    /// </summary>
    /// <param name="key">Identificador del vehículo que se desea restaurar.</param>
    /// <returns>El vehículo restaurado o null si no existe.</returns>
    Task<VehiculoModel?> Restore(string key);
}
