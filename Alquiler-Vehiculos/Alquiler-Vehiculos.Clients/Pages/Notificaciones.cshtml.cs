using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Alquiler_Vehiculos.Clients.Pages;

[Authorize(Roles = "Trabajador,Responsable,Admin")]
public class NotificacionesModel : PageModel {
    public void OnGet() {
    }
}