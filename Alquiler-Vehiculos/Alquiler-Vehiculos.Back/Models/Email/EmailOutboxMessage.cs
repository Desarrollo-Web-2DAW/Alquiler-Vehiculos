namespace Alquiler_Vehiculos.Models.Email;

/// <summary>
///     Mensaje de correo persistido en la cola de envío (patrón outbox).
///     Garantiza que un fallo del servidor de correo nunca rompe la operación
///     de negocio (RF-35) y que cada evento se envía exactamente una vez (CU-17)
///     gracias a <see cref="ClaveIdempotencia" />.
/// </summary>
public class EmailOutboxMessage {
    /// <summary>Identificador interno del mensaje.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Tipo de notificación (decide la plantilla usada).</summary>
    public required TipoNotificacion Tipo { get; init; }

    /// <summary>
    ///     Clave única del evento de negocio que originó el correo.
    ///     Impide duplicados: si ya existe un mensaje con esta clave, no se encola otro.
    /// </summary>
    public required string ClaveIdempotencia { get; init; }

    /// <summary>Correo del destinatario.</summary>
    public required string DestinatarioEmail { get; init; }

    /// <summary>Nombre del destinatario.</summary>
    public required string DestinatarioNombre { get; init; }

    /// <summary>Asunto del correo.</summary>
    public required string Asunto { get; init; }

    /// <summary>Cuerpo del correo en HTML (ya renderizado desde su plantilla).</summary>
    public required string CuerpoHtml { get; init; }

    /// <summary>Estado actual del envío.</summary>
    public EstadoEmailOutbox Estado { get; set; } = EstadoEmailOutbox.Pendiente;

    /// <summary>Número de intentos de envío realizados.</summary>
    public int Intentos { get; set; }

    /// <summary>Último error registrado (para diagnóstico y reintentos).</summary>
    public string? UltimoError { get; set; }

    /// <summary>Fecha y hora de creación del mensaje.</summary>
    public DateTimeOffset FechaCreacion { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Fecha y hora del próximo intento (backoff exponencial).</summary>
    public DateTimeOffset? ProximoIntento { get; set; }

    /// <summary>Fecha y hora del envío efectivo.</summary>
    public DateTimeOffset? FechaEnvio { get; set; }
}
