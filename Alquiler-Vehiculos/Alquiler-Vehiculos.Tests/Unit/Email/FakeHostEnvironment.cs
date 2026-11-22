using Alquiler_Vehiculos.Services.Email;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Alquiler_Vehiculos.Tests.Unit.Email;

/// <summary>
/// Entorno falso para probar <see cref="EmailTemplateRenderer" /> sin ASP.NET Core.
/// Apunta la raíz de contenido a la carpeta del proyecto Back, donde viven las
/// plantillas de <c>Templates/Email</c>.
/// </summary>
public class FakeHostEnvironment(string contentRootPath) : IHostEnvironment {
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "Alquiler-Vehiculos.Tests";
    public string ContentRootPath { get; set; } = contentRootPath;
    public IFileProvider ContentRootFileProvider { get; set; } = null!;

    /// <summary>
    /// Localiza la carpeta del proyecto Back subiendo desde la carpeta de salida
    /// de los tests hasta encontrar las plantillas de correo.
    /// </summary>
    public static FakeHostEnvironment ParaPlantillas() {
        return new FakeHostEnvironment(LocalizarProyectoBack());
    }

    private static string LocalizarProyectoBack() {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta is not null) {
            var candidato = Path.Combine(carpeta.FullName, "Alquiler-Vehiculos.Back");
            if (Directory.Exists(Path.Combine(candidato, "Templates", "Email"))) {
                return candidato;
            }
            carpeta = carpeta.Parent;
        }
        throw new DirectoryNotFoundException(
            "No se encontró la carpeta Alquiler-Vehiculos.Back con sus plantillas de correo.");
    }
}
