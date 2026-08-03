using Microsoft.AspNetCore.Identity;

namespace BasketBaseTracker.Web.Data;

public static class IdentitySeeder
{
    private const string RolAdministrador = "Administrador";

    // Idempotente: si ya existe algún administrador, no hace nada (architecture.md
    // punto 7 — sin autorregistro, solo esta primera cuenta nace de un seed).
    public static async Task SeedAdministradorAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        if (!await roleManager.RoleExistsAsync(RolAdministrador))
        {
            await roleManager.CreateAsync(new IdentityRole(RolAdministrador));
        }

        if ((await userManager.GetUsersInRoleAsync(RolAdministrador)).Count > 0)
        {
            return;
        }

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "No existe ningún administrador y faltan las credenciales de seed ('Seed:AdminEmail' / " +
                "'Seed:AdminPassword'). Configúralas con 'dotnet user-secrets set' (skill dotnet) antes de arrancar.");
        }

        // UserName = Email: el login (Login.cshtml.cs) autentica pasando el email como
        // nombre de usuario a PasswordSignInAsync, así que deben coincidir.
        var administrador = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var resultado = await userManager.CreateAsync(administrador, password);
        if (!resultado.Succeeded)
        {
            throw new InvalidOperationException(
                $"No se pudo crear el administrador inicial: {string.Join("; ", resultado.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(administrador, RolAdministrador);
    }
}
