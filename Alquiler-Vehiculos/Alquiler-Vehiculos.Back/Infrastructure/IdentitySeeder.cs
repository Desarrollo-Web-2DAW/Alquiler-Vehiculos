using Alquiler_Vehiculos.Enums.Usuarios;
using Microsoft.AspNetCore.Identity;

namespace Alquiler_Vehiculos.Infrastructure;

public static class IdentitySeeder {
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration) {
        using var scope = serviceProvider.CreateScope();
        await SeedRolesAsync(scope.ServiceProvider);
        await SeedAdminUserAsync(scope.ServiceProvider, configuration); 
    }
    
    private static async Task SeedRolesAsync(IServiceProvider serviceProvider) {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var roles = Enum.GetNames(typeof(RolUsuario));
        
        foreach (var role in roles) {
            if (!await roleManager.RoleExistsAsync(role)) {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }
    
    private static async Task SeedAdminUserAsync(IServiceProvider serviceProvider, IConfiguration configuration) {
        var userManager = serviceProvider.GetRequiredService<UserManager<Models.Usuarios.User>>();
        var adminEmail = configuration["AdminUser:Email"];
        var adminPassword = configuration["AdminUser:Password"];
        
        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword)) {
            throw new InvalidOperationException("Admin user email or password is not configured.");
        }
        
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null) {
            adminUser = new Models.Usuarios.User {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (!result.Succeeded) {
                throw new InvalidOperationException($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        
        if (!await userManager.IsInRoleAsync(adminUser, nameof(RolUsuario.Admin))) {
            await userManager.AddToRoleAsync(adminUser, nameof(RolUsuario.Admin));
        }
    }
}

