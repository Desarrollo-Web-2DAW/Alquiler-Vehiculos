using FluentValidation;

namespace Alquiler_Vehiculos.Validators.Pagos;

/// <summary>
/// Datos de entrada para validar un pago con tarjeta de crédito (RF-21, RF-22).
/// El CVC solo se valida en memoria y JAMÁS se persiste ni se registra en logs.
/// </summary>
public record DatosTarjetaDto(
    string TipoMedio,
    string NumeroTarjeta,
    int MesCaducidad,
    int AnioCaducidad,
    string Cvc);

/// <summary>
/// Validador de tarjeta de crédito y algoritmo de Luhn (ISO/IEC 7812-1, RF-21).
/// </summary>
public class LuhnValidator : AbstractValidator<DatosTarjetaDto> {
    public LuhnValidator(TimeProvider? timeProvider = null) {
        var reloj = timeProvider ?? TimeProvider.System;

        RuleFor(x => x.TipoMedio)
            .NotEmpty().WithMessage("El medio de pago es obligatorio")
            .Must(m => string.Equals(m?.Trim(), "Credito", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Solo se admite el pago con tarjeta de crédito");

        RuleFor(x => x.NumeroTarjeta)
            .NotEmpty().WithMessage("El número de tarjeta es obligatorio")
            .Must(EsLuhnValido).WithMessage("El número de tarjeta no supera la validación de Luhn");

        RuleFor(x => x.MesCaducidad)
            .InclusiveBetween(1, 12).WithMessage("El mes de caducidad debe estar entre 1 y 12");

        RuleFor(x => x)
            .Must(x => !EstaCaducada(x.MesCaducidad, x.AnioCaducidad, reloj.GetUtcNow().UtcDateTime))
            .WithName("caducidad")
            .WithMessage("La tarjeta de crédito está caducada");

        RuleFor(x => x.Cvc)
            .NotEmpty().WithMessage("El CVC es obligatorio")
            .Matches(@"^\d{3,4}$").WithMessage("El CVC debe tener 3 o 4 dígitos");
    }

    /// <summary>
    /// Algoritmo de Luhn (módulo 10, RF-21): recorre de derecha a izquierda duplicando
    /// cada segundo dígito desde el penúltimo (restando 9 si >= 10) y exige suma % 10 == 0.
    /// Acepta 11..19 dígitos para cubrir también el ejemplo 79927398713 del enunciado.
    /// </summary>
    public static bool EsLuhnValido(string? numero) {
        if (string.IsNullOrWhiteSpace(numero)) return false;

        var digitos = LimpiarNumero(numero);
        if (digitos.Length < 11 || digitos.Length > 19 || !digitos.All(char.IsDigit)) {
            return false;
        }

        var suma = 0;
        var duplicar = false;

        for (var i = digitos.Length - 1; i >= 0; i--) {
            var d = digitos[i] - '0';
            if (duplicar) {
                d *= 2;
                if (d >= 10) d -= 9;
            }
            suma += d;
            duplicar = !duplicar;
        }

        return suma % 10 == 0;
    }

    public static string LimpiarNumero(string numero) =>
        new(numero.Where(c => c != ' ' && c != '-').ToArray());

    private static bool EstaCaducada(int mes, int anio, DateTime ahoraUtc) {
        if (mes is < 1 or > 12 || anio < 2000 || anio > 2100) return true;
        var finDeMes = new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes), 23, 59, 59, DateTimeKind.Utc);
        return finDeMes < ahoraUtc;
    }
}