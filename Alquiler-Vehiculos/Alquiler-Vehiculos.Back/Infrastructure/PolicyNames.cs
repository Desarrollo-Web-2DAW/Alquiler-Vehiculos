namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
/// Nombres de las políticas de autorización (RF-46).
/// Se usan con [Authorize(Policy = PolicyNames.Admin)].
/// </summary>
public static class PolicyNames {
    public const string Admin = "RequireAdminRole";
    public const string Cliente = "RequireClienteRole";
    public const string Trabajador = "RequireTrabajadorRole";
    public const string Responsable = "RequireResponsableRole";
    public const string Mecanico = "RequireMecanico";
}
