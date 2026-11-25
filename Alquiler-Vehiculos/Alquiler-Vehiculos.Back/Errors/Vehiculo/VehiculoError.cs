using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;

namespace Alquiler_Vehiculos.Errors.Vehiculo;

/// <summary>
/// Contiene los errores de dominio relacionados con la entidad Vehículo.
/// </summary>
public static class VehiculoError
{
    /// <summary>
    /// Crea un error de tipo <see cref="NotFoundError"/> cuando no se encuentra un vehículo por su identificador.
    /// </summary>
    /// <param name="id">Identificador del vehículo (clave en formato string).</param>
    /// <returns>Una instancia de <see cref="NotFoundError"/> (HTTP 404).</returns>
    public static NotFoundError NotFoundVehiculo(object id)
    {
        return new NotFoundError("VEHICULO_NO_ENCONTRADO", $"Vehículo con ID '{id}' no encontrado");
    }

    /// <summary>
    /// Crea un error de tipo <see cref="ConflictError"/> cuando se intenta registrar un vehículo con una matrícula que ya existe.
    /// </summary>
    /// <param name="matricula">Matrícula duplicada.</param>
    /// <returns>Una instancia de <see cref="ConflictError"/> (HTTP 409).</returns>
    public static ConflictError VehiculoYaExiste(string matricula)
    {
        return new ConflictError("MATRICULA_DUPLICADA", $"Ya existe un vehículo con matrícula '{matricula}'");
    }

    public static ConflictError NoActualizable(string id)
    {
        return new ConflictError("VEHICULO_NO_ACTUALIZABLE", $"No se puede actualizar el vehículo '{id}' porque está dado de baja.");
    }

    /// <summary>
    /// Crea un error cuando no se encuentra un vehículo por su matrícula.
    /// </summary>
    /// <param name="matricula">Matrícula del vehículo buscado.</param>
    /// <returns>Una instancia de <see cref="NotFoundError"/> (HTTP 404).</returns>
    public static NotFoundError NotFoundVehiculoMatricula(string matricula)
    {
        return new NotFoundError("VEHICULO_NO_ENCONTRADO", $"Vehículo con matrícula '{matricula}' no encontrado");
    }

    /// <summary>
    /// Crea un error de tipo <see cref="BadRequestError"/> cuando la primera letra del código ACRISS no corresponde a una categoría válida.
    /// </summary>
    /// <returns>Una instancia de <see cref="BadRequestError"/> (HTTP 400).</returns>
    public static ValidationError TipoDeCarroceria()
    {
        return ValidationError.Create("El Acriss dado no es correcto respecto al catálogo, tipo de Categoría no válido");
    }
    
    /// <summary>
    /// Crea un error de tipo <see cref="BadRequestError"/> cuando la tercera letra del código ACRISS no corresponde a un combustible válido.
    /// </summary>
    /// <returns>Una instancia de <see cref="BadRequestError"/> (HTTP 400).</returns>
    public static ValidationError TipoCombustible()
    {
        return ValidationError.Create("El Acriss dado no es correcto respecto al catálogo, tipo de Combustible no válido");
    }
    
    /// <summary>
    /// Crea un error de tipo <see cref="BadRequestError"/> cuando la cuarta letra del código ACRISS no corresponde a una transmisión válida.
    /// </summary>
    /// <returns>Una instancia de <see cref="BadRequestError"/> (HTTP 400).</returns>
    public static ValidationError TipoDeTransmision()
    {
        return ValidationError.Create("El Acriss dado no es correcto respecto al catálogo, tipo de Transmision no válido");
    }

    /// <summary>
    /// Crea un error de tipo <see cref="BadRequestError"/> cuando el color especificado no coincide con un tipo de color permitido.
    /// </summary>
    /// <returns>Una instancia de <see cref="BadRequestError"/> (HTTP 400).</returns>
    public static ValidationError TipoColorInvalido()
    {
        return ValidationError.Create("El color especificado no es válido en el catálogo.");
    }

    /// <summary>
    /// Crea un error de tipo <see cref="ConflictError"/> cuando se intenta cambiar el estado de un vehículo infringiendo reglas de negocio.
    /// </summary>
    /// <param name="actual">Estado actual del vehículo.</param>
    /// <param name="pedido">Estado solicitado.</param>
    /// <returns>Una instancia de <see cref="ConflictError"/> (HTTP 409).</returns>
    public static ConflictError CambiarDeEstado(string actual, string pedido)
    {
        return new ConflictError(
            "ESTADO_VEHICULO_INVALIDO",
            $"No se puede cambiar el estado del vehiculo debido a restricciones de negocio: EstadoActual:{actual}-EstadoPedido:{pedido}");
    }

    /// <summary>
    /// Crea un error de tipo <see cref="ConflictError"/> cuando se intenta dar de baja un vehículo
    /// que no está <see cref="EstadoVehiculo.Disponible"/> (RF-01).
    /// </summary>
    /// <param name="estado">Estado actual del vehículo.</param>
    /// <returns>Una instancia de <see cref="ConflictError"/> (HTTP 409).</returns>
    public static ConflictError NoDisponibleParaBaja(EstadoVehiculo estado)
    {
        return new ConflictError("VEHICULO_NO_DISPONIBLE", $"Solo se puede dar de baja un vehículo Disponible. Estado actual: {estado}.");
    }

    /// <summary>
    /// Crea un error de tipo <see cref="ConflictError"/> cuando se intenta restaurar un vehículo
    /// que no está retirado (RF-01 y RF-05).
    /// </summary>
    /// <param name="estado">Estado actual del vehículo.</param>
    /// <returns>Una instancia de <see cref="ConflictError"/> (HTTP 409).</returns>
    public static ConflictError NoRetiradoParaRestaurar(EstadoVehiculo estado)
    {
        return new ConflictError("VEHICULO_NO_RETIRADO", $"Solo se puede restaurar un vehículo retirado. Estado actual: {estado}.");
    }

    /// <summary>
    /// Crea un error de tipo <see cref="ConflictError"/> cuando se intenta dar de baja un vehículo
    /// que tiene un parte de taller abierto (RF-01 y RF-08).
    /// </summary>
    /// <param name="id">Identificador del vehículo.</param>
    /// <returns>Una instancia de <see cref="ConflictError"/> (HTTP 409).</returns>
    public static ConflictError VehiculoConPartesAbiertos(string id)
    {
        return new ConflictError("VEHICULO_CON_PARTE_ABIERTO", $"No se puede dar de baja el vehículo '{id}' porque tiene un parte de taller abierto.");
    }

    /// <summary>
    /// Crea un error de tipo <see cref="BusinessRuleError"/> cuando ocurre un error genérico al realizar el borrado del vehículo.
    /// </summary>
    /// <returns>Una instancia de <see cref="BusinessRuleError"/> (HTTP 400/422).</returns>
    public static BusinessRuleError BorradoError()
    {
        return new BusinessRuleError("BORRADO_VEHICULO", "No se ha podido completar la operación de borrado del vehículo.");
    }
}
