using Alquiler_Vehiculos.Models.Email;

namespace Alquiler_Vehiculos.Services.Email;

/// <summary>
///     Renderiza las plantillas HTML de <c>Templates/Email</c> sustituyendo
///     los marcadores <c>{{Propiedad}}</c> y los bloques de lista
///     <c>{{#each Coleccion}} ... {{/each}}</c>.
/// </summary>
public interface IEmailTemplateRenderer {
    /// <summary>
    ///     Renderiza la plantilla del tipo de notificación con los datos indicados.
    /// </summary>
    /// <param name="tipo">Tipo de notificación (elige la plantilla).</param>
    /// <param name="datos">Objeto con las propiedades que encajan con los marcadores.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>HTML final del correo.</returns>
    Task<string> RenderizarAsync(TipoNotificacion tipo, object datos, CancellationToken cancellationToken = default);
}
