using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Models.Vehiculo.Enums.ACRISS;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Mappers.Vehiculo;

/// <summary>
/// Clase estática que sirve como base para poder crear métodos de extensión para intercambiar datos entre distintas entidades de <see cref="Vehiculo"/>
/// </summary>
public static class VehiculoMapper
{
    /// <summary>
    /// Parte de un <see cref="CreateVehiculoDto"/> para crear un <see cref="Vehiculo"/> modelo
    /// </summary>
    /// <param name="dto">Data Object Transfer con los datos proporcionados por el cliente</param>
    /// <param name="foto">El modelo de foto asociado al vehiculo</param>
    /// <returns><see cref="Vehiculo"/></returns>
    public static Result<VehiculoModel, DomainError> ToVehiculo(this CreateVehiculoDto dto, Foto foto)
    {
        var carroceria = ObtenerTipoCarroceria(dto.Acriss[1],dto.Acriss[0]);
        if (carroceria.IsFailure) return carroceria.Error;
        var combustible = ObtenerTipoCombustible(dto.Acriss[2]);
        if (combustible.IsFailure) return combustible.Error;
        var transmision = ObtenerTipoTransmision(dto.Acriss[3]);
        if (transmision.IsFailure) return transmision.Error;
        
        return new VehiculoModel()
        {
            FotoFk = dto.Foto,
            Foto = foto,
            Matrícula = dto.Matricula,
            Marca = dto.Marca,
            Modelo = dto.Modelo,
            Acriss = dto.Acriss,
            Carroceria = carroceria.Value,
            Combustible = combustible.Value,
            Transmision = transmision.Value,
            Estado = dto.IsActivo ? EstadoVehiculo.Disponible : EstadoVehiculo.Retirado,
            Color = (TipoColor)Enum.Parse(typeof(TipoColor), dto.Color, ignoreCase: true),
            PrecioBaseDia = dto.PrecioBaseDia,
            Plazas = dto.Plazas,
            MaleteroLitros = dto.MaleteroLitros,
            AutonomiaKm = dto.AutonomiaKm,
            Kilometraje = dto.Kilometraje,
            FechaUltimaRevision = DateTime.TryParse(dto.FechaUltimaRevision, out var fecha) ? fecha : DateTime.UtcNow,
            ConesionarioFk = dto.Concesionario,
            Activo = dto.IsActivo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            DeletedAt = null
        };
    }
    
    /// <summary>
    /// Parte de un <see cref="Vehiculo"/> para crear un <see cref="VehiculoDto"/>
    /// </summary>
    /// <param name="vehiculoModel">El modelo de vehículo</param>
    /// <returns><see cref="VehiculoDto"/></returns>
    public static VehiculoDto ToDto(this Models.Vehiculo.VehiculoModel vehiculoModel)
    {
        return new VehiculoDto
        {
            Foto = vehiculoModel.FotoFk,
            Matricula = vehiculoModel.Matrícula,
            Marca = vehiculoModel.Marca,
            Modelo = vehiculoModel.Modelo,
            Acriss = vehiculoModel.Acriss,
            Categoria = vehiculoModel.Carroceria.ToString(),
            Combustible = vehiculoModel.Combustible.ToString(),
            Transmision = vehiculoModel.Transmision.ToString(),
            Estado = vehiculoModel.Estado.ToString(),
            Color = vehiculoModel.Color.ToString(),
            PrecioBaseDia = vehiculoModel.PrecioBaseDia,
            Plazas = vehiculoModel.Plazas,
            MaleteroLitros = vehiculoModel.MaleteroLitros,
            AutonomiaKm = vehiculoModel.AutonomiaKm,
            Kilometraje = vehiculoModel.Kilometraje,
            FechaUltimaRevision = vehiculoModel.FechaUltimaRevision.ToString("yyyy-MM-dd"), // O el formato de fecha que prefieras
            Concesionario = vehiculoModel.ConesionarioFk,
            IsActivo = vehiculoModel.Activo
        };
    }
    
    /// <summary>
    /// Obtiene <see cref="TipoCarroceria"/> según la segunda letra del ACRISS y siguiendo las restricciones del enunciado
    /// utilizando para comprobar la primera para saber si es un Hatchback si la segunda letra no coincide con nigún tiopo de carrocería del catálogo
    /// comprobandose este con la primera letra del ACRISS
    /// </summary>
    /// <param name="letra2">AcrissCategoria</param>
    /// <param name="letra1">AcrissTipo</param>
    /// <returns><see cref="TipoCarroceria"/> o <see cref="BadRequestError"/></returns>
    private static Result<TipoCarroceria, DomainError> ObtenerTipoCarroceria( char letra2, char letra1)
    {
        var acrissCategoria = (AcrissCategoria)letra2;
        if (!Enum.TryParse(acrissCategoria.ToString(), out TipoCarroceria categoria))
        {
            Enum.TryParse(letra1.ToString().AsSpan(), out AcrissTipo tipo);
            if (tipo != AcrissTipo.DosCuatroPuertas && tipo != AcrissTipo.DosTresPuertas && tipo != AcrissTipo.CuatroCincoPuertas)
            {
                return VehiculoError.TipoDeCarroceria();
            }
            return TipoCarroceria.Hatchback;
        }
        return categoria;
    }
    
    /// <summary>
    /// Obtiene <see cref="TipoCombustible"/> según la cuarta letra del ACRISS
    /// </summary>
    /// <param name="letra4">AcrissCombustible</param>
    /// <returns><see cref="TipoCombustible"/> o <see cref="BadRequestError"/></returns>
    private static Result<TipoCombustible, DomainError> ObtenerTipoCombustible( char letra4)
    {
        var acrrisTipoCombustible = (AcrissCombustibleClimatizacion)letra4;
        
        TipoCombustible[] valores = Enum.GetValues<TipoCombustible>();

        foreach (var valor in valores)
        {
            if (acrrisTipoCombustible.ToString().Contains(valor.ToString())) return valor;
        }
        
        return VehiculoError.TipoCombustible();
    }
    
    
    /// <summary>
    /// Obtiene <see cref="TipoCombustible"/> según la tercera letra del ACRISS
    /// </summary>
    /// <param name="letra3">AcrissTransmision</param>
    /// <returns><see cref="TipoCombustible"/> o <see cref="BadRequestError"/></returns>
    private static Result<TipoTransmision, DomainError> ObtenerTipoTransmision( char letra3)
    {
        var acrissTipoTransmision = (AcrissTransmisionTraccion)letra3;
        
        TipoTransmision[] valores = Enum.GetValues<TipoTransmision>();

        foreach (var valor in valores)
        {
            if (acrissTipoTransmision.ToString().Contains(valor.ToString())) return valor;
        }


        return VehiculoError.TipoDeTransmision();
    }
}