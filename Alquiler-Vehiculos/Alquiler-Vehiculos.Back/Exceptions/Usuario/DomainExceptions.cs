namespace Alquiler_Vehiculos.Exceptions;


/// <summary>
/// Excepción para violaciones de reglas de negocio (HTTP 400/422).
/// </summary>
public class BusinessException : Exception
{
    /// <summary>Crea excepción con mensaje.</summary>
    /// <param name="message">Mensaje descriptivo.</param>
    public BusinessException(string message) : base(message)
    {
    }

    /// <summary>Crea excepción con inner exception.</summary>
    /// <param name="message">Mensaje descriptivo.</param>
    /// <param name="innerException">Excepción interna.</param>
    public BusinessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Excepción para recursos no encontrados (HTTP 404).
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>Crea excepción con mensaje.</summary>
    /// <param name="message">Mensaje descriptivo.</param>
    public NotFoundException(string message) : base(message)
    {
    }

    /// <summary>Crea excepción con inner exception.</summary>
    /// <param name="message">Mensaje descriptivo.</param>
    /// <param name="innerException">Excepción interna.</param>
    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Excepción para errores de validación de datos (HTTP 400).
/// </summary>
public class ValidationException : Exception
{
    /// <summary>Errores de validación por campo.</summary>
    public Dictionary<string, string[]> Errors { get; }

    /// <summary>Crea excepción con mensaje simple.</summary>
    /// <param name="message">Mensaje de error.</param>
    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>Crea excepción con errores por campo.</summary>
    /// <param name="message">Mensaje de error.</param>
    /// <param name="errors">Diccionario de errores por campo.</param>
    public ValidationException(string message, Dictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }

    /// <summary>Crea excepción con inner exception.</summary>
    /// <param name="message">Mensaje de error.</param>
    /// <param name="innerException">Excepción interna.</param>
    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = new Dictionary<string, string[]>();
    }
}

/// <summary>
/// Excepción para errores de serialización en PostgreSQL (HTTP 500).
/// Se usa en el enfoque híbrido Serializable + Retry.
/// </summary>
public class SerializationFailureException : Exception
{
    /// <summary>Crea excepción con mensaje.</summary>
    /// <param name="message">Mensaje descriptivo.</param>
    public SerializationFailureException(string message) : base(message)
    {
    }

    /// <summary>Crea excepción con inner exception.</summary>
    /// <param name="message">Mensaje descriptivo.</param>
    /// <param name="innerException">Excepción interna.</param>
    public SerializationFailureException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}