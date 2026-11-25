using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Storage;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;

namespace Alquiler_Vehiculos.Services.Storage;

/// <summary>
/// Define las operaciones que necesita la API para gestionar los archivos y sus rutas.
/// </summary>
public interface IStorageService 
{
    /// <summary>
    /// Almacena el archivo recibido en la carpeta indicada y devuelve su modelo asociado.
    /// </summary>
    /// <param name="file">Archivo enviado a la API que se quiere almacenar.</param>
    /// <param name="folder">Carpeta del almacenamiento en la que se guardará.</param>
    /// <param name="fileName">El nombre definitivo del archivo.</param>
    /// <returns>El modelo <see cref="Foto"/> creado a partir de los datos del archivo, o un error de dominio.</returns>
    Task<Result<Foto, DomainError>> SaveFileAsync(
        IFormFile file, string folder, string fileName, CancellationToken ct = default);
    

    /// <summary>
    /// Borra del almacenamiento el archivo identificado por su ruta relativa o nombre.
    /// </summary>
    /// <param name="filename">Ruta relativa o nombre que permite localizar el archivo que se quiere borrar.</param>
    /// <returns>Éxito con true si no hay excepciones, incluso si el archivo ya no existe; error si falla el borrado.</returns>
    Task<Result<bool, DomainError>> DeleteFileAsync(string filename);

    /// <summary>
    /// Indica si el archivo indicado se encuentra en el almacenamiento.
    /// </summary>
    /// <param name="filename">Nombre o ruta relativa del archivo que se quiere buscar.</param>
    /// <returns>True si se encuentra el archivo; false si no existe.</returns>
    bool FileExists(string filename);

    /// <summary>
    /// Resuelve la ubicación completa del archivo en el disco del servidor.
    /// </summary>
    /// <param name="filename">Nombre o ruta relativa a partir de la que se localizará el archivo.</param>
    /// <returns>La ruta física completa, no una dirección web.</returns>
    string GetFullPath(string filename);

    /// <summary>
    /// Construye la referencia al archivo que se puede guardar en la base de datos,
    /// sin incluir la ruta física del servidor.
    /// </summary>
    /// <param name="filename">Nombre que tendrá el archivo en la referencia.</param>
    /// <param name="folder">Carpeta a la que pertenece; se utiliza "vehiculos" por defecto si no se indica otra.</param>
    /// <returns>Una referencia con la estructura /uploads/{carpeta}/{nombre}.</returns>
    string GetRelativePath(string filename, string folder = "vehiculos");

    /// <summary>
    /// Detecta si el archivo es nulo o está vacío.
    /// </summary>
    /// <param name="file">El archivo que se quiere comprobar.</param>
    /// <returns>True si está vacío o es nulo, False en caso contrario.</returns>
    bool IsEmpty(IFormFile? file);
}