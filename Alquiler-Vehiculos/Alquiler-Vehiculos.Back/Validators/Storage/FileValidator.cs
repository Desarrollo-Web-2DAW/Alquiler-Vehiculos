using Alquiler_Vehiculos.Config;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Alquiler_Vehiculos.Validators.Storage;

/// <summary>
/// Validador encargado de comprobar que un archivo (<see cref="IFormFile"/>) cumple 
/// con las reglas de negocio y políticas de almacenamiento definidas en la configuración.
/// </summary>
public class FileValidator : AbstractValidator<IFormFile>
{
    private readonly int _maxFileSize;
    private readonly List<string> _allowedExtensions;
    private readonly List<string> _allowedContentTypes;
    
    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="FileValidator"/>,
    /// cargando las restricciones de tamaño, extensiones y tipos permitidos desde la configuración.
    /// </summary>
    /// <param name="settings">Objeto de configuración <see cref="StorageSettings"/> con los parámetros permitidos.</param>
    public FileValidator(StorageSettings settings)
    {
        _maxFileSize = settings.MaxFileSize;
        _allowedExtensions = settings.AllowedExtensions;
        _allowedContentTypes = settings.AllowedContentTypes;

        // 1. Regla: El archivo no debe ser nulo ni tener un tamaño de 0 bytes
        RuleFor(file => file)
            .NotNull().WithMessage("Debe proporcionar una imagen con contenido.")
            .Must(file => file.Length > 0).WithMessage("Debe proporcionar una imagen con contenido.");

        // 2. Reglas condicionales: Se ejecutan únicamente si el archivo no es nulo
        When(file => file != null, () =>
        {
            // Regla: El tamaño del archivo no debe superar el límite máximo configurado
            RuleFor(file => file.Length)
                .LessThanOrEqualTo(_maxFileSize)
                .WithMessage("El archivo es demasiado grande.");

            // Regla: La extensión del archivo debe estar incluida en la lista blanca permitida
            RuleFor(file => Path.GetExtension(file.FileName).ToLowerInvariant())
                .Must(ext => _allowedExtensions.Contains(ext))
                .WithMessage("La extensión del archivo no está permitida.");

            // Regla: El tipo de contenido (ContentType) debe ser válido y estar permitido
            RuleFor(file => file.ContentType.ToLowerInvariant())
                .NotNull()
                .Must(ct => ct != null && _allowedContentTypes.Contains(ct, StringComparer.OrdinalIgnoreCase))
                .WithMessage("El tipo de contenido no está permitido.");

            // Regla: El nombre del archivo debe ser seguro para evitar vulnerabilidades de Path Traversal
            RuleFor(file => file.FileName)
                .Must(filename => 
                {
                    var name = Path.GetFileName(filename);
                    return !name.Contains("..") && !name.Contains('/') && !name.Contains('\\');
                })
                .WithMessage("El nombre del archivo no es válido.");
        });
    }
}