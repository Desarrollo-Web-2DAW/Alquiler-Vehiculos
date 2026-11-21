using Alquiler_Vehiculos.Errors.Common;

namespace Alquiler_Vehiculos.Errors.Vehiculo;

/// <summary>
/// Contiene los errores de dominio relacionados con la entidad Vehículo.
/// </summary>
public static class VehiculoError
{
    /// <summary>
    /// Crea un error de tipo <see cref="NotFoundError"/> cuando no se encuentra un vehículo por su identificador.
    /// </summary>
    /// <param name="id">Identificador del vehículo (puede ser ID numérico o clave en formato string).</param>
    /// <returns>Una instancia de <see cref="NotFoundError"/> (HTTP 404).</returns>
    public static NotFoundError NotFoundVehiculo(object id)
    {
        return NotFoundError.FromId(Convert.ToInt64(id), "Vehículo");
    }

    /// <summary>
    /// Crea un error de tipo <see cref="ConflictError"/> cuando se intenta registrar un vehículo con una matrícula que ya existe.
    /// </summary>
    /// <param name="matricula">Matrícula duplicada.</param>
    /// <returns>Una instancia de <see cref="ConflictError"/> (HTTP 409).</returns>
    public static ConflictError VehiculoYaExiste(string matricula)
    {
        return ConflictError.Duplicate("Vehículo", matricula);
    }

    /// <summary>
    /// Crea un error de tipo <see cref="BadRequestError"/> cuando la primera letra del código ACRISS no corresponde a una categoría válida.
    /// </summary>
    /// <returns>Una instancia de <see cref="BadRequestError"/> (HTTP 400).</returns>
    public static BadRequestError TipoDeCarroceria()
    {
        return new BadRequestError("El Acriss dado no es correcto respecto al catálogo, tipo de Categoría no válido");
    }
    
    /// <summary>
    /// Crea un error de tipo <see cref="BadRequestError"/> cuando la tercera letra del código ACRISS no corresponde a un combustible válido.
    /// </summary>
    /// <returns>Una instancia de <see cref="BadRequestError"/> (HTTP 400).</returns>
    public static BadRequestError TipoCombustible()
    {
        return new BadRequestError("El Acriss dado no es correcto respecto al catálogo, tipo de Combustible no válido");
    }
    
    /// <summary>
    /// Crea un error de tipo <see cref="BadRequestError"/> cuando la cuarta letra del código ACRISS no corresponde a una transmisión válida.
    /// </summary>
    /// <returns>Una instancia de <see cref="BadRequestError"/> (HTTP 400).</returns>
    public static BadRequestError TipoDeTransmision()
    {
        return new BadRequestError("El Acriss dado no es correcto respecto al catálogo, tipo de Transmision no válido");
    }

    /// <summary>
    /// Crea un error de tipo <see cref="BadRequestError"/> cuando el color especificado no coincide con un tipo de color permitido.
    /// </summary>
    /// <returns>Una instancia de <see cref="BadRequestError"/> (HTTP 400).</returns>
    public static BadRequestError TipoColorInvalido()
    {
        return new BadRequestError("El color especificado no es válido en el catálogo.");
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
            $"No se puede cambiar el estado del vehiculo debido a restricciones de negocio: EstadoActual:{actual}-EstadoPedido:{pedido}");
    }

    /// <summary>
    /// Crea un error de tipo <see cref="BusinessRuleError"/> cuando ocurre un error genérico al realizar el borrado del vehículo.
    /// </summary>
    /// <returns>Una instancia de <see cref="BusinessRuleError"/> (HTTP 400/422).</returns>
    public static BusinessRuleError BorradoError()
    {
        return new BusinessRuleError("No se ha podido completar la operación de borrado del vehículo.");
    }
}