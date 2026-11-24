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

        if (int.TryParse(dto.Color, out _) || !Enum.TryParse<TipoColor>(dto.Color, true, out var color)
            || !Enum.IsDefined(color)) return VehiculoError.TipoColorInvalido();

        // RF-01: un vehículo nuevo nace siempre Disponible (valor por defecto del modelo) y activo.
        if (!DateTime.TryParse(dto.FechaUltimaRevision, out var fecha) || fecha > DateTime.UtcNow)
            return ValidationError.Create("La fecha de la última revisión debe ser válida y no puede ser futura.");
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
            Color = color,
            PrecioBaseDia = dto.PrecioBaseDia,
            Plazas = dto.Plazas,
            MaleteroLitros = dto.MaleteroLitros,
            AutonomiaKm = dto.AutonomiaKm,
            Kilometraje = dto.Kilometraje,
            // Npgsql solo admite DateTime UTC en columnas 'timestamp with time zone'.
            FechaUltimaRevision = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc),
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

        if (int.TryParse(dto.Color, out _) || !Enum.TryParse<TipoColor>(dto.Color, true, out var color)
            || !Enum.IsDefined(color)) return VehiculoError.TipoColorInvalido();

        if (!DateTime.TryParse(dto.FechaUltimaRevision, out var fecha) || fecha > DateTime.UtcNow)
            return ValidationError.Create("La fecha de la última revisión debe ser válida y no puede ser futura.");

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
            Color = color,
            PrecioBaseDia = dto.PrecioBaseDia,
            Plazas = dto.Plazas,
            MaleteroLitros = dto.MaleteroLitros,
            AutonomiaKm = dto.AutonomiaKm,
            Kilometraje = dto.Kilometraje,
            // Npgsql solo admite DateTime UTC en columnas 'timestamp with time zone'.
            FechaUltimaRevision = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc),
            ConesionarioFk = dto.Concesionario,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            DeletedAt = null
        };
        
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
            Id = vehiculoModel.Id,
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
    /// equivalente en <see cref="TipoCarroceria"/> aparece aquí. Las letras del catálogo sin
    /// carrocería propia se aproximan a la más cercana de la flota: <c>Deportivo</c> a
    /// <c>Coupe</c>, <c>Roadster</c> a <c>Descapotable</c>, <c>Crossover</c> a <c>Suv</c>,
    /// pickups a <c>TodoTerreno</c> y autocaravanas/recreativos a <c>Furgoneta</c>.
    /// Solo quedan fuera los códigos comerciales <c>Especial</c> y <c>OfertaEspecial</c>,
    /// que devuelven error de carrocería (ver la memoria de la práctica).
    /// </remarks>
    /// <param name="letraTipo">Segunda letra del ACRISS (catálogo <see cref="AcrissTipo"/>).</param>
    /// <returns><see cref="TipoCarroceria"/> o <see cref="BadRequestError"/></returns>
    internal static Result<TipoCarroceria, DomainError> ObtenerTipoCarroceria(char letraTipo)
    {
        return (AcrissTipo)letraTipo switch
        {
            AcrissTipo.DosTresPuertas
                or AcrissTipo.DosCuatroPuertas
                or AcrissTipo.CuatroCincoPuertas => TipoCarroceria.Hatchback,
            AcrissTipo.Berlina                => TipoCarroceria.Sedan,
            AcrissTipo.Familiar               => TipoCarroceria.Familiar,
            AcrissTipo.FurgonetaPasajeros
                or AcrissTipo.FurgonetaComercial
                or AcrissTipo.Autocaravana
                or AcrissTipo.Recreativo    => TipoCarroceria.Furgoneta,
            AcrissTipo.Suv
                or AcrissTipo.Crossover     => TipoCarroceria.Suv,
            AcrissTipo.TodoTerrenoAbierto
                or AcrissTipo.PickupCabinaSimple
                or AcrissTipo.PickupDobleCabina => TipoCarroceria.TodoTerreno,
            AcrissTipo.Descapotable
                or AcrissTipo.Roadster     => TipoCarroceria.Descapotable,
            AcrissTipo.Coupe
                or AcrissTipo.Deportivo    => TipoCarroceria.Coupe,
            AcrissTipo.Monovolumen          => TipoCarroceria.Monovolumen,
            _                                 => VehiculoError.TipoDeCarroceria()
        };
    }
    
    /// <summary>
    /// Obtiene <see cref="TipoCombustible"/> según la cuarta letra del ACRISS
    /// </summary>
    /// <remarks>
    /// <see cref="TipoCombustible"/> solo admite cuatro valores (RF-04), así que las letras
    /// del catálogo sin equivalente exacto se agrupan en el valor más cercano: GLP/gas
    /// comprimido, multicombustible y etanol se clasifican como <c>Gasolina</c>; el hidrógeno
    /// (pila de combustible) como <c>Electrico</c>; y «sin especificar» toma <c>Gasolina</c>
    /// como valor por defecto de la flota. Se documenta en la memoria de la práctica.
    /// </remarks>
    /// <param name="letraCombustible">AcrissCombustible</param>
    /// <returns><see cref="TipoCombustible"/> o <see cref="BadRequestError"/></returns>
    internal static Result<TipoCombustible, DomainError> ObtenerTipoCombustible(char letraCombustible)
    {
        return letraCombustible switch
        {
            'D' or 'Q' => TipoCombustible.Diesel,
            'V' or 'Z' or 'L' or 'S' or 'M' or 'F' or 'U' or 'X' or 'R' or 'N'
                => TipoCombustible.Gasolina,
            'H' or 'I' => TipoCombustible.Hibrido,
            'E' or 'C' or 'A' or 'B' => TipoCombustible.Electrico,
            _ => VehiculoError.TipoCombustible()
        };
    }
    
    
    /// <summary>
    /// Obtiene <see cref="TipoTransmision"/> según la tercera letra del ACRISS
    /// </summary>
    /// <remarks>
    /// Los niveles de automatización 3, 4 y 5 (<c>Q</c>, <c>H</c> y <c>F</c>) son cajas
    /// automáticas, así que se mapean a <see cref="TipoTransmision.Automatica"/>.
    /// </remarks>
    /// <param name="letraTransmision">AcrissTransmision</param>
    /// <returns><see cref="TipoTransmision"/> o <see cref="BadRequestError"/></returns>
    internal static Result<TipoTransmision, DomainError> ObtenerTipoTransmision(char letraTransmision)
    {
        return letraTransmision switch
        {
            'M' or 'N' or 'C' => TipoTransmision.Manual,
            'A' or 'B' or 'D' or 'Q' or 'H' or 'F' => TipoTransmision.Automatica,
            _ => VehiculoError.TipoDeTransmision()
        };
    }
}
