using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Errors.Common;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;

public interface IVehiculoRepository
{
    // Create devuelve el conflicto del índice de matrícula, sin cambiar el CRUD de otros módulos.
    Task<Result<VehiculoModel, DomainError>> Create(VehiculoModel item);
    Task<VehiculoModel?> GetById(string key);
    Task<bool> SoftDelete(string key);
    Task<VehiculoModel?> Update(string key, VehiculoModel item);
    Task<IEnumerable<VehiculoModel>> GetAll();

    /// <summary>
    /// Comprueba si un vehiculo existe en base a su Id
    /// </summary>
    /// <param name="key">El id</param>
    /// <returns>True si existe, false si no existe</returns>
    Task<bool> ExistById(string key);
    
    /// <summary>
    /// Comprueba si un vehiculo existe en base a su matricula
    /// </summary>
    /// <param name="matricula">La matricula</param>
    /// <returns>True si existe, false si no existe</returns>
    Task<bool> ExistByMatricula(string matricula);
    
    /// <summary>
    /// Obtiene un vehiculo en base a su matricula
    /// </summary>
    /// <param name="matricula">La matricula</param>
    /// <returns><see cref="VehiculoModel"/> si existe, null si no existe</returns>
    Task<VehiculoModel?> GetByMatricula(string matricula);
    
    /// <summary>
    /// Cuenta los registros y recupera únicamente la página solicitada desde la base de datos.
    /// </summary>
    /// <param name="page">Número de página, desde 1.</param>
    /// <param name="pageSize">Número máximo de elementos por página.</param>
    /// <returns>Elementos de la página y total de registros.</returns>
    Task<(IEnumerable<VehiculoModel> Items, int TotalItems)> GetPaged(int page, int pageSize);
    
    /// <summary>
    /// Restaura una Vehiculo por su Id
    /// </summary>
    /// <param name="key">El id</param>
    /// <returns><see cref="VehiculoModel"/>si existe, null si no existe</returns>
    Task<VehiculoModel?> Restore(string key);

    /// <summary>
    /// Da de baja lógica el vehículo <b>solo si</b> su estado actual en la base de datos
    /// es <paramref name="estadoEsperado"/>.
    /// </summary>
    /// <remarks>
    /// Implementa un compare-and-set: la comprobación del estado viaja en el <c>WHERE</c>
    /// del <c>UPDATE</c>, de modo que "decidir" y "escribir" ocurren en la misma sentencia.
    /// Si otro proceso cambió la fila entre la lectura y la escritura, no coincide ninguna
    /// y se devuelve <c>false</c>; ese <c>false</c> es una respuesta de negocio, no un error técnico.
    /// </remarks>
    /// <param name="key">Identificador del vehículo.</param>
    /// <param name="estadoEsperado">Estado que debe tener el vehículo en ese instante.</param>
    /// <returns><c>true</c> si se actualizó alguna fila; <c>false</c> si ninguna coincidió.</returns>
    Task<bool> SoftDeleteSiEstado(string key, EstadoVehiculo estadoEsperado);
}
