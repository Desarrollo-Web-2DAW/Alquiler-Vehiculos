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
        var carroceria = ObtenerTipoCarroceria(dto.Acriss[1]);
        if (carroceria.IsFailure) return carroceria.Error;
        var combustible = ObtenerTipoCombustible(dto.Acriss[3]);
        if (combustible.IsFailure) return combustible.Error;
        var transmision = ObtenerTipoTransmision(dto.Acriss[2]);
        if (transmision.IsFailure) return transmision.Error;

        // RF-01: un vehículo nuevo nace siempre Disponible (valor por defecto del modelo) y activo.
        return new VehiculoModel()
        {
            FotoFk = foto.Nombre,
            Foto = foto,
            Matrícula = dto.Matricula,
            Marca = dto.Marca,
            Modelo = dto.Modelo,
            Acriss = dto.Acriss,
            Carroceria = carroceria.Value,
            Combustible = combustible.Value,
            Transmision = transmision.Value,
            Color = (TipoColor)Enum.Parse(typeof(TipoColor), dto.Color, ignoreCase: true),
            PrecioBaseDia = dto.PrecioBaseDia,
            Plazas = dto.Plazas,
            MaleteroLitros = dto.MaleteroLitros,
            AutonomiaKm = dto.AutonomiaKm,
            Kilometraje = dto.Kilometraje,
            FechaUltimaRevision = DateTime.TryParse(dto.FechaUltimaRevision, out var fecha) ? fecha : DateTime.UtcNow,
            ConesionarioFk = dto.Concesionario,
            Activo = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            DeletedAt = null
        };
    }

    /// <summary>
    /// Parte de un <see cref="UpdateVehiculoDto"/> para crear un nuevo <see cref="VehiculoModel"/>
    /// </summary>
    /// <param name="dto">Datos proporcionados por el cliente</param>
    /// <param name="foto">El modelo de foto opcional asociado al vehículo</param>
    /// <returns><see cref="VehiculoModel"/> o <see cref="DomainError"/></returns>
    public static Result<VehiculoModel, DomainError> ToVehiculoModel(this UpdateVehiculoDto dto, Foto foto)
    {
        var carroceria = ObtenerTipoCarroceria(dto.Acriss[1]);
        if (carroceria.IsFailure) return carroceria.Error;
        
        var combustible = ObtenerTipoCombustible(dto.Acriss[3]);
        if (combustible.IsFailure) return combustible.Error;
        
        var transmision = ObtenerTipoTransmision(dto.Acriss[2]);
        if (transmision.IsFailure) return transmision.Error;

        var estado = Enum.Parse<EstadoVehiculo>(dto.Estado, ignoreCase: true);

        var vehiculo = new VehiculoModel
        {
            FotoFk = foto.Nombre,
            Foto = foto,
            Matrícula = dto.Matricula,
            Marca = dto.Marca,
            Modelo = dto.Modelo,
            Acriss = dto.Acriss,
            Carroceria = carroceria.Value,
            Combustible = combustible.Value,
            Transmision = transmision.Value,
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
        
        vehiculo.CambiarEstadoMapper(estado);
        return vehiculo;
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
    /// Obtiene <see cref="TipoCarroceria"/> a partir de la segunda letra del ACRISS,
    /// que se interpreta como <see cref="AcrissTipo"/>.
    /// </summary>
    /// <remarks>
    /// El mapa es explícito a propósito: cada miembro de <see cref="AcrissTipo"/> que tiene
    /// equivalente en <see cref="TipoCarroceria"/> aparece aquí. Los que no lo tienen
    /// (<c>Deportivo</c>, <c>Roadster</c>, <c>Crossover</c>, pickups, autocaravanas,
    /// recreativos y los códigos comerciales <c>Especial</c>/<c>OfertaEspecial</c>)
    /// devuelven error de carrocería.
    /// </remarks>
    /// <param name="letraTipo">Segunda letra del ACRISS (catálogo <see cref="AcrissTipo"/>).</param>
    /// <returns><see cref="TipoCarroceria"/> o <see cref="BadRequestError"/></returns>
    private static Result<TipoCarroceria, DomainError> ObtenerTipoCarroceria(char letraTipo)
    {
        return (AcrissTipo)letraTipo switch
        {
            AcrissTipo.DosTresPuertas
                or AcrissTipo.DosCuatroPuertas
                or AcrissTipo.CuatroCincoPuertas => TipoCarroceria.Hatchback,
            AcrissTipo.Berlina                => TipoCarroceria.Sedan,
            AcrissTipo.Familiar               => TipoCarroceria.Familiar,
            AcrissTipo.FurgonetaPasajeros
                or AcrissTipo.FurgonetaComercial => TipoCarroceria.Furgoneta,
            AcrissTipo.Suv                    => TipoCarroceria.Suv,
            AcrissTipo.TodoTerrenoAbierto     => TipoCarroceria.TodoTerreno,
            AcrissTipo.Descapotable           => TipoCarroceria.Descapotable,
            AcrissTipo.Coupe                  => TipoCarroceria.Coupe,
            AcrissTipo.Monovolumen            => TipoCarroceria.Monovolumen,
            _                                 => VehiculoError.TipoDeCarroceria()
        };
    }
    
    /// <summary>
    /// Obtiene <see cref="TipoCombustible"/> según la cuarta letra del ACRISS
    /// </summary>
    /// <param name="letraCombustible">AcrissCombustible</param>
    /// <returns><see cref="TipoCombustible"/> o <see cref="BadRequestError"/></returns>
    private static Result<TipoCombustible, DomainError> ObtenerTipoCombustible(char letraCombustible)
    {
        var acrrisTipoCombustible = (AcrissCombustibleClimatizacion)letraCombustible;
        
        TipoCombustible[] valores = Enum.GetValues<TipoCombustible>();

        foreach (var valor in valores)
        {
            if (acrrisTipoCombustible.ToString().Contains(valor.ToString())) return valor;
        }
        
        return VehiculoError.TipoCombustible();
    }
    
    
    /// <summary>
    /// Obtiene <see cref="TipoTransmision"/> según la tercera letra del ACRISS
    /// </summary>
    /// <param name="letraTransmision">AcrissTransmision</param>
    /// <returns><see cref="TipoTransmision"/> o <see cref="BadRequestError"/></returns>
    private static Result<TipoTransmision, DomainError> ObtenerTipoTransmision(char letraTransmision)
    {
        var acrissTipoTransmision = (AcrissTransmisionTraccion)letraTransmision;
        
        TipoTransmision[] valores = Enum.GetValues<TipoTransmision>();

        foreach (var valor in valores)
        {
            if (acrissTipoTransmision.ToString().Contains(valor.ToString())) return valor;
        }
        
        return VehiculoError.TipoDeTransmision();
    }
}