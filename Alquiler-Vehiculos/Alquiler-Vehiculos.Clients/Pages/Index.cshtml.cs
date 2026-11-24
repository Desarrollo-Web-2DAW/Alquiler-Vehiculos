using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Alquiler_Vehiculos.Clients.Pages;

public class IndexModel : PageModel {
    public IActionResult OnGet() {
        if (User.Identity?.IsAuthenticated == true &&
            (User.IsInRole("Trabajador") || User.IsInRole("Responsable") || User.IsInRole("Admin"))) {
            return RedirectToPage("/Notificaciones");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostLogoutAsync() {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Index");
    }
}
