namespace Alquiler_Vehiculos.Dto.Email;

/// <summary>
///     Datos comunes a cualquier notificación por correo al cliente.
///     Los nombres de las propiedades son los marcadores de las plantillas
///     HTML: <c>{{CodigoReferencia}}</c>, <c>{{ClienteNombre}}</c>, etc.
/// </summary>
public abstract record NotificacionEmailBase {
    /// <summary>Código de referencia de la reserva (aparece en el asunto y el cuerpo).</summary>
    public required string CodigoReferencia { get; init; }

    /// <summary>Nombre del cliente destinatario.</summary>
    public required string ClienteNombre { get; init; }

    /// <summary>Correo del cliente destinatario.</summary>
    public required string ClienteEmail { get; init; }

    /// <summary>Nombre de la oficina (pie del correo).</summary>
    public string OficinaNombre { get; init; } = "Oficina Central";

    /// <summary>Dirección de la oficina (pie del correo).</summary>
    public string OficinaDireccion { get; init; } = string.Empty;

    /// <summary>Teléfono de la oficina (pie del correo).</summary>
    public string OficinaTelefono { get; init; } = string.Empty;

    /// <summary>Horario de la oficina (pie del correo y recogidas).</summary>
    public string OficinaHorario { get; init; } = string.Empty;

    /// <summary>
    ///     Clave de idempotencia del evento. Si es null se genera como
    ///     <c>{TipoNotificacion}:{CodigoReferencia}</c>, lo que garantiza
    ///     exactamente un correo por evento (CU-17).
    /// </summary>
    public string? ClaveIdempotencia { get; init; }

    /// <summary>Asunto del correo.</summary>
    public abstract string Asunto { get; }
}
