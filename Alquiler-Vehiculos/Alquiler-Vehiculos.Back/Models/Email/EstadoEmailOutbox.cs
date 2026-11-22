namespace Alquiler_Vehiculos.Models.Email;

/// <summary>
///     Estado de un mensaje en la cola de correos (outbox).
/// </summary>
public enum EstadoEmailOutbox {
    /// <summary>En cola, pendiente de envío o de reintento.</summary>
    Pendiente,

    /// <summary>Entregado al servidor de correo.</summary>
    Enviado,

    /// <summary>Agotó los reintentos: queda registrado y pendiente de revisión manual.</summary>
    Fallido
}
