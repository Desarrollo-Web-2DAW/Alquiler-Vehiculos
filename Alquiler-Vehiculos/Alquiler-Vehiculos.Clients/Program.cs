using Alquiler_Vehiculos.Clients.Services.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = "/Auth/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpClient("AlquilerVehiculosApi",
    client => { client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!); });

builder.Services.AddScoped<AuthApiService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.MapGet("/auth/signalr-token", (HttpContext context) => {
    var token = context.User.FindFirst("access_token")?.Value;

    if (string.IsNullOrEmpty(token)) return Results.Unauthorized();

    context.Response.Headers.CacheControl = "no-store";

    return Results.Ok(new { accessToken = token });
}).RequireAuthorization();

app.Run();