using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Alquiler_Vehiculos.Models.Email;

namespace Alquiler_Vehiculos.Services.Email;

/// <summary>
///     Renderizador de plantillas HTML de <c>Templates/Email</c>.
///     Soporta marcadores de propiedad (<c>{{ClienteNombre}}</c>) y bloques
///     de lista (<c>{{#each Lineas}} ... {{/each}}</c>). Los valores se
///     formatean en castellano (fechas dd/MM/yyyy y decimales con dos cifras).
/// </summary>
public partial class EmailTemplateRenderer(IHostEnvironment environment) : IEmailTemplateRenderer {
    private static readonly ConcurrentDictionary<string, string> _cache = new();
    private static readonly CultureInfo _cultura = CultureInfo.GetCultureInfo("es-ES");

    [GeneratedRegex(@"\{\{#each\s+(\w+)\s*\}\}([\s\S]*?)\{\{/each\}\}")]
    private static partial Regex BloqueEachRegex();

    [GeneratedRegex(@"\{\{\s*(\w+)\s*\}\}")]
    private static partial Regex PlaceholderRegex();

    /// <inheritdoc />
    public async Task<string> RenderizarAsync(
        TipoNotificacion tipo, object datos, CancellationToken cancellationToken = default) {
        var plantilla = await CargarPlantillaAsync(tipo, cancellationToken);
        var conListas = ExpandirBloques(plantilla, datos);
        return ReemplazarPlaceholders(conListas, datos);
    }

    private Task<string> CargarPlantillaAsync(TipoNotificacion tipo, CancellationToken cancellationToken) {
        var nombre = NombrePlantilla(tipo);
        var plantilla = _cache.GetOrAdd(nombre, _ => {
            var ruta = Path.Combine(environment.ContentRootPath, "Templates", "Email", nombre);
            return File.ReadAllText(ruta, Encoding.UTF8);
        });
        return Task.FromResult(plantilla);
    }

    /// <summary>
    ///     Nombre del fichero de plantilla de cada tipo:
    ///     <c>ReservaCreada</c> → <c>reserva-creada.html</c>.
    /// </summary>
    public static string NombrePlantilla(TipoNotificacion tipo) {
        var kebab = Regex.Replace(tipo.ToString(), "(?<=[a-z0-9])([A-Z])", "-$1").ToLowerInvariant();
        return kebab + ".html";
    }

    private static string ExpandirBloques(string plantilla, object datos) {
        return BloqueEachRegex().Replace(plantilla, match => {
            var propiedad = match.Groups[1].Value;
            var bloque = match.Groups[2].Value;
            var valor = datos.GetType().GetProperty(propiedad)?.GetValue(datos);

            if (valor is not System.Collections.IEnumerable elementos || valor is string) {
                return string.Empty;
            }

            var resultado = new StringBuilder();
            foreach (var elemento in elementos) {
                resultado.Append(ReemplazarPlaceholders(bloque, elemento));
            }

            return resultado.ToString();
        });
    }

    private static string ReemplazarPlaceholders(string plantilla, object datos) {
        return PlaceholderRegex().Replace(plantilla,
            match => Formatear(datos.GetType().GetProperty(match.Groups[1].Value)?.GetValue(datos)));
    }

    private static string Formatear(object? valor) {
        return valor switch {
            null => string.Empty,
            string texto => texto,
            bool si => si ? "Sí" : "No",
            DateOnly fecha => fecha.ToString("dd/MM/yyyy", _cultura),
            DateTime fecha => fecha.ToString("dd/MM/yyyy HH:mm", _cultura),
            DateTimeOffset fecha => fecha.ToString("dd/MM/yyyy HH:mm", _cultura),
            decimal importe => importe.ToString("N2", _cultura),
            double numero => numero.ToString("N2", _cultura),
            float numero => numero.ToString("N2", _cultura),
            _ => valor.ToString() ?? string.Empty
        };
    }
}
