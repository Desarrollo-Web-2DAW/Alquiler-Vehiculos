
﻿using Microsoft.AspNetCore.Http;

namespace Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;

/// <summary>
/// Datos de entrada para crear un vehículo del catálogo.
/// </summary>
public record CreateVehiculoDto {
    /// <summary>Archivo de foto que se asociará al vehículo.</summary>
    public IFormFile? Foto { get; set; }
    /// <summary>Matrícula del vehículo.</summary>
    public string Matricula { get; set; } = string.Empty;
    /// <summary>Nombre de la marca del vehículo.</summary>
    public string Marca { get; set; } = string.Empty;
    /// <summary>Nombre del modelo del vehículo.</summary>
    public string Modelo { get; set; } = string.Empty;
    /// <summary>Código ACRISS que clasifica el vehículo.</summary>
    public string Acriss { get; set; } = string.Empty;
    /// <summary>Color del vehículo.</summary>
    public string Color { get; set; } = string.Empty;
    /// <summary>Precio base de alquiler por día.</summary>
    public decimal PrecioBaseDia { get; set; }
    /// <summary>Número de plazas del vehículo.</summary>
    public int Plazas { get; set; }
    /// <summary>Capacidad del maletero expresada en litros.</summary>
    public int MaleteroLitros { get; set; }
    /// <summary>Autonomía del vehículo expresada en kilómetros.</summary>
    public int AutonomiaKm { get; set; }
    /// <summary>Kilómetros recorridos por el vehículo.</summary>
    public int Kilometraje { get; set; }
    /// <summary>Fecha de la última revisión representada como texto.</summary>
    public string FechaUltimaRevision { get; set; } = string.Empty;
    /// <summary>Identificador del concesionario en el que se adquirió el vehículo.</summary>
    public long Concesionario { get; set; }
}