using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;

namespace Alquiler_Vehiculos.Models.Oficina;

/// <summary>
/// Oficina única de la empresa de alquiler (RF-11).
/// Se siembra una sola vez con <see cref="OficinaUnicaId"/> y siempre debe tener un responsable de Operaciones.
/// </summary>
public class Oficina {
    /// <summary>Identificador fijo que garantiza una única oficina en la base de datos.</summary>
    public static readonly Guid OficinaUnicaId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Guid Id { get; set; } = OficinaUnicaId;
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public DayOfWeek DiaInicio { get; set; } = DayOfWeek.Monday;
    public DayOfWeek DiaFin { get; set; } = DayOfWeek.Friday;
    public TimeOnly HoraApertura { get; set; } = new(9, 0);
    public TimeOnly HoraCierre { get; set; } = new(20, 0);

    /// <summary>Trabajador de Operaciones designado como responsable actual (obligatorio, CU-18).</summary>
    public Guid ResponsableId { get; set; }
    public Trabajador Responsable { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Comprueba si una fecha/hora de recogida o devolución cae dentro del horario (CU-22).</summary>
    public bool EstaAbierta(DateTime fechaHora) {
        var dia = OrdenSemana(fechaHora.DayOfWeek);
        var dentroDeDias = dia >= OrdenSemana(DiaInicio) && dia <= OrdenSemana(DiaFin);
        var hora = TimeOnly.FromDateTime(fechaHora);

        return dentroDeDias && hora >= HoraApertura && hora <= HoraCierre;
    }

    /// <summary>Texto legible del horario para la API y las plantillas de correo (RF-34).</summary>
    public string HorarioFormateado =>
        $"{NombreDia(DiaInicio)} a {NombreDia(DiaFin)} de {HoraApertura:HH\\:mm} a {HoraCierre:HH\\:mm}";

    public static int OrdenSemana(DayOfWeek dia) => dia == DayOfWeek.Sunday ? 7 : (int)dia;

    private static string NombreDia(DayOfWeek dia) => dia switch {
        DayOfWeek.Monday => "Lunes",
        DayOfWeek.Tuesday => "Martes",
        DayOfWeek.Wednesday => "Miércoles",
        DayOfWeek.Thursday => "Jueves",
        DayOfWeek.Friday => "Viernes",
        DayOfWeek.Saturday => "Sábado",
        DayOfWeek.Sunday => "Domingo",
        _ => dia.ToString()
    };
}
