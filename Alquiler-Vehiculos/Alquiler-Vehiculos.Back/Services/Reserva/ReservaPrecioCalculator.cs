using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Enums;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Reserva;
using Alquiler_Vehiculos.Validators.Reservas;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Reserva;

/// <summary>Entrada mínima de un extra para calcular líneas (sin depender de la entidad Extra).</summary>
public record ExtraSeleccionado(string Nombre, decimal PrecioDiario, int Cantidad);

/// <summary>Línea calculada lista para persistir en <c>LineaReserva</c> (RF-25).</summary>
public record LineaCalculada(
    TipoLineaReserva Tipo,
    string Concepto,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal);

/// <summary>Desglose completo con garantía de <c>PrecioTotal == Lineas.Sum(l => l.Subtotal)</c>.</summary>
public record PresupuestoReserva(
    int Dias,
    IReadOnlyList<LineaCalculada> Lineas,
    decimal PrecioTotal);

/// <summary>
/// Cálculo puro de edad en recogida, líneas, recargo conductor joven (25 %),
/// devolución tardía y reembolsos (RF-13, RF-25, CU-09, CU-13, CU-24, CU-26).
/// </summary>
public static class ReservaPrecioCalculator {
    /// <summary>Calcula la edad exacta del cliente el día de la recogida (RF-13, CU-26).</summary>
    public static int CalcularEdadEnFecha(DateOnly fechaNacimiento, DateOnly fechaRecogida) {
        var edad = fechaRecogida.Year - fechaNacimiento.Year;
        if (fechaNacimiento.AddYears(edad) > fechaRecogida) {
            edad--;
        }
        return edad;
    }

    /// <summary>
    /// Construye todas las líneas de la reserva y su total exacto (RF-13, RF-25, CU-26).
    /// Rechaza con 400 si el cliente tiene menos de 18 años el día de la recogida.
    /// </summary>
    public static Result<PresupuestoReserva, DomainError> Calcular(
        DateOnly fechaNacimientoCliente,
        DateTime fechaRecogida,
        DateTime fechaDevolucion,
        string conceptoVehiculo,
        decimal precioBaseDiaCongelado,
        string nombrePoliza,
        decimal incrementoDiarioPoliza,
        IReadOnlyList<ExtraSeleccionado>? extras = null,
        int diasRetraso = 0) {
        var edadEnRecogida = CalcularEdadEnFecha(fechaNacimientoCliente, DateOnly.FromDateTime(fechaRecogida));
        if (edadEnRecogida < ReservasConstantes.EdadMinimaAlquiler) {
            return Result.Failure<PresupuestoReserva, DomainError>(ReservaError.ClienteMenorDeEdad());
        }

        var diasBase = FechasReservaValidator.CalcularDias(fechaRecogida, fechaDevolucion);
        var diasVehiculo = diasBase + Math.Max(0, diasRetraso); // CU-24: +1 día por cada día de retraso

        var lineas = new List<LineaCalculada>();

        // 1. Línea obligatoria de vehículo (tarifa diaria congelada al crear la reserva, RF-25)
        var subtotalVehiculo = Redondear(diasVehiculo * precioBaseDiaCongelado);
        lineas.Add(new LineaCalculada(
            TipoLineaReserva.Vehiculo,
            conceptoVehiculo,
            diasVehiculo,
            precioBaseDiaCongelado,
            subtotalVehiculo));

        // 2. Línea obligatoria de póliza (básica = 0 €/día, RF-23)
        var subtotalPoliza = Redondear(diasBase * incrementoDiarioPoliza);
        lineas.Add(new LineaCalculada(
            TipoLineaReserva.Poliza,
            nombrePoliza,
            diasBase,
            incrementoDiarioPoliza,
            subtotalPoliza));

        // 3. Líneas opcionales de extras (PrecioDiario * días * unidades, RF-24)
        if (extras is not null) {
            foreach (var extra in extras.Where(e => e.Cantidad > 0)) {
                var cantidadTotal = diasBase * extra.Cantidad;
                var subtotalExtra = Redondear(cantidadTotal * extra.PrecioDiario);
                lineas.Add(new LineaCalculada(
                    TipoLineaReserva.Extra,
                    $"{extra.Nombre} (x{extra.Cantidad})",
                    cantidadTotal,
                    extra.PrecioDiario,
                    subtotalExtra));
            }
        }

        // 4. Recargo conductor joven: 25 % sobre la línea de vehículo si < 25 años el día de la recogida (RF-13, CU-26)
        if (edadEnRecogida < ReservasConstantes.EdadSinRecargoJoven) {
            var importeRecargo = Redondear(subtotalVehiculo * ReservasConstantes.PorcentajeRecargoConductorJoven);
            lineas.Add(new LineaCalculada(
                TipoLineaReserva.Recargo,
                "Recargo conductor menor de 25 años (25 %)",
                1,
                importeRecargo,
                importeRecargo));
        }

        var total = Redondear(lineas.Sum(l => l.Subtotal));
        return Result.Success<PresupuestoReserva, DomainError>(new PresupuestoReserva(diasVehiculo, lineas, total));
    }

    /// <summary>CU-09: en anulación de reserva pagada se devuelve el importe íntegro.</summary>
    public static decimal CalcularReembolsoCancelacion(decimal importePagado) => Redondear(importePagado);

    /// <summary>CU-13: en NoPresentado se devuelve el importe menos 1 día de alquiler.</summary>
    public static decimal CalcularReembolsoNoPresentado(decimal importePagado, decimal tarifaDiariaVehiculo) {
        var penalizacion = tarifaDiariaVehiculo * ReservasConstantes.DiasPenalizacionNoPresentado;
        return Redondear(Math.Max(0m, importePagado - penalizacion));
    }

    private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}