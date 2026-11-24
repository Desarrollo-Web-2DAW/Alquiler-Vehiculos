using System.Security.Claims;
using Alquiler_Vehiculos.Clients.Models.Auth;
using Alquiler_Vehiculos.Clients.Services.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Alquiler_Vehiculos.Clients.Pages.Auth;

public class LoginModel(AuthApiService authApiService) : PageModel {
    [BindProperty] public LoginRequest LoginRequest { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken) {
        if (!ModelState.IsValid) return Page();

        try {
            var result = await authApiService.LoginAsync(LoginRequest, cancellationToken);

            if (result is null) {
                ErrorMessage = "Correo electrónico o contraseña incorrectos.";
                return Page();
            }

            if (result.Role is not ("Trabajador" or "Responsable" or "Admin")) {
                return RedirectToPage("/Auth/AccesoDenegado");
            }

            var claims = new List<Claim> {
                new(ClaimTypes.Name, result.Email),
                new(ClaimTypes.Email, result.Email),
                new(ClaimTypes.Role, result.Role),
                new("access_token", result.Token)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            var properties = new AuthenticationProperties {
                IsPersistent = false,
                ExpiresUtc = result.ExpiresAt,
                AllowRefresh = false
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);

            return RedirectToPage("/Notificaciones");
        }
        catch (HttpRequestException) {
            ErrorMessage = "No se ha podido conectar con el servidor.";
            return Page();
        }
    }
}