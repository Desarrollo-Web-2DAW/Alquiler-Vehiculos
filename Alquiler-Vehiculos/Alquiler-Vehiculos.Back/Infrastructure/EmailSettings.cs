namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
///     Configuración del servicio de correo (sección <c>Email</c> de la configuración).
///     En desarrollo apunta a Mailpit (contenedor local, sin credenciales);
///     en producción, al servidor SMTP real con credenciales fuera del repositorio (RF-36).
/// </summary>
public class EmailSettings {
    /// <summary>Nombre de la sección de configuración.</summary>
    public const string Seccion = "Email";

    /// <summary>Servidor SMTP.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Puerto SMTP (Mailpit usa el 1025).</summary>
    public int Port { get; set; } = 1025;

    /// <summary>Usuario SMTP (vacío en desarrollo: Mailpit no exige autenticación).</summary>
    public string? Username { get; set; }

    /// <summary>Contraseña SMTP (nunca en el repositorio; variable de entorno en producción).</summary>
    public string? Password { get; set; }

    /// <summary>Correo del remitente.</summary>
    public string From { get; set; } = "no-reply@alquilervehiculos.local";

    /// <summary>Nombre visible del remitente.</summary>
    public string FromName { get; set; } = "Alquiler Vehículos";

    /// <summary>Seguridad de la conexión: <c>None</c>, <c>Auto</c>, <c>Ssl</c> o <c>Tls</c>.</summary>
    public string Security { get; set; } = "None";

    /// <summary>
    ///     <c>true</c> envía de verdad por SMTP (Mailpit en desarrollo, servidor real en
    ///     producción); <c>false</c> usa el servicio simulado en memoria (pruebas).
    /// </summary>
    public bool UseSmtp { get; set; } = true;

    /// <summary>Máximo de intentos de envío antes de marcar el correo como fallido.</summary>
    public int MaxIntentos { get; set; } = 5;

    /// <summary>Segundos base del backoff exponencial de reintentos (30, 60, 120...).</summary>
    public int IntervaloBaseReintentosSegundos { get; set; } = 30;

    /// <summary>Segundos entre pasadas del worker sobre la cola de envío.</summary>
    public int FrecuenciaProcesoSegundos { get; set; } = 10;

    /// <summary>Número máximo de correos procesados por pasada del worker.</summary>
    public int LoteMaximo { get; set; } = 20;
}
