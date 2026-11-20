using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Models.Vehiculo;

public record VehiculoModel
{
    public string Id { get; init; } = null!;
    public string FotoFk { get; init; } = string.Empty;
    public Foto Foto { get; init; } = null!;
    public string Matrícula { get; init; } = string.Empty;
    public string Marca { get; init; } = string.Empty;
    public string Modelo { get; init; } = string.Empty;
    public string Acriss { get; init; } = string.Empty;
    public TipoCarroceria Carroceria { get; init; }
    public TipoCombustible Combustible { get; init; }
    public TipoTransmision Transmision { get; init; }
    public EstadoVehiculo Estado { get; private set; } = EstadoVehiculo.Disponible;
    public TipoColor Color { get; init; }
    public decimal PrecioBaseDia { get; init; }
    public int Plazas { get; init; }
    public int MaleteroLitros { get; init; }
    public int AutonomiaKm { get; init; }
    public int Kilometraje { get; init; }
    public DateTime FechaUltimaRevision { get; init; }
    public long ConesionarioFk { get; init; }
    public bool Activo { get; init; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Result<bool, DomainError> MarcarVehiculoDisponible()
    {
        if (Estado != EstadoVehiculo.Retirado)
        {
            Estado = EstadoVehiculo.Disponible;
            return true;
        }

        return Result.Failure<bool, DomainError>(VehiculoError.CambiarDeEstado(this.Estado.ToString(), nameof(EstadoVehiculo.Disponible)));
    }
    
    public Result<bool, DomainError> MarcarVehiculoReservado(EstadoVehiculo estado)
    {
        if (Estado != EstadoVehiculo.Retirado && Estado == EstadoVehiculo.Disponible)
        {
            Estado = EstadoVehiculo.Reservado;
            return true;
        }
        return Result.Failure<bool, DomainError>(VehiculoError.CambiarDeEstado(this.Estado.ToString(),  nameof(EstadoVehiculo.Reservado)));
    }
    
    public Result<bool, DomainError> MarcarVehiculoAlquilado()
    {
        if (Estado != EstadoVehiculo.Retirado  && Estado == EstadoVehiculo.Reservado)
        {
            Estado = EstadoVehiculo.EnAlquiler;
            return true;
        }
        return Result.Failure<bool, DomainError>(VehiculoError.CambiarDeEstado(this.Estado.ToString(), nameof(EstadoVehiculo.EnAlquiler)));
    }
    
    public Result<bool, DomainError> MeterVehiculoTaller()
    {
        if (Estado != EstadoVehiculo.Retirado)
        {
            Estado = EstadoVehiculo.Mantenimiento;
            return true;
        }
        return Result.Failure<bool, DomainError>(VehiculoError.CambiarDeEstado(this.Estado.ToString(), nameof(EstadoVehiculo.Mantenimiento)));
    }

    protected void CambiarEstadoMapper(EstadoVehiculo estado)
    {
        Estado = estado;
    }
    
}