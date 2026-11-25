using Alquiler_Vehiculos.Models.Vehiculo;

namespace Alquiler_Vehiculos.Services.Vehiculo.Transactions;

public sealed record UpdateVehiculoResult(VehiculoModel Vehiculo, string MatriculaAnterior);
