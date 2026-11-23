namespace Alquiler_Vehiculos.Dto.Email;

/// <summary>
///     Línea de la reserva tal y como se muestra en los correos (RF-25).
///     Se repite dentro de las plantillas con <c>{{#each Lineas}}</c>.
/// </summary>
/// <param name="Concepto">Concepto de la línea (vehículo, póliza, extra o recargo).</param>
/// <param name="Cantidad">Unidades o días de la línea.</param>
/// <param name="PrecioUnitario">Tarifa unitaria fijada al reservar.</param>
/// <param name="Subtotal">Cantidad por precio unitario.</param>
public sealed record LineaEmailDto(string Concepto, int Cantidad, decimal PrecioUnitario, decimal Subtotal);
