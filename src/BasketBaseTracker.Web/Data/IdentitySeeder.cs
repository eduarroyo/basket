using Microsoft.AspNetCore.Identity;

namespace BasketBaseTracker.Web.Data;

public static class IdentitySeeder
{
    private const string RolGestorCompeticion = "GestorCompeticion";
    private const string RolAdministrador = "Administrador";

    // Idempotente: si ya existe algún administrador, no hace nada salvo la
    // migración de roles (architecture.md punto 7 — sin autorregistro, solo esta
    // primera cuenta nace de un seed).
    public static async Task SeedAdministradorAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await MigrarRolesAsync(roleManager, userManager);

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

        // La cuenta nace con ambos roles: gestiona la competición y, al ser la única
        // cuenta de sistema disponible, también import/export (BAS-16, spec.md).
        await userManager.AddToRolesAsync(administrador, [RolGestorCompeticion, RolAdministrador]);
    }

    // BAS-16: el rol único "Administrador" de la v1 se divide en "GestorCompeticion"
    // (resto del área Admin) y un "Administrador" nuevo, más restringido (solo
    // import/export). Migración idempotente, pensada para correr sola en cada
    // arranque de contenedor sin ningún paso manual contra la base de datos de
    // producción (spec.md, Aclaraciones). Pública para poder probar directamente el
    // camino de migración de un rol legado sin depender del orden de arranque del
    // AppHost de test (ya sembrado en fresco antes de que corra el test).
    public static async Task MigrarRolesAsync(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
    {
        var gestorCompeticionExiste = await roleManager.RoleExistsAsync(RolGestorCompeticion);
        var administradorLegado = !gestorCompeticionExiste ? await roleManager.FindByNameAsync(RolAdministrador) : null;

        if (administradorLegado is not null)
        {
            // Renombrar en sitio conserva AspNetUserRoles intacto — nadie pierde su
            // rol actual, solo cambia el nombre bajo el que se conoce.
            var usuariosLegado = await userManager.GetUsersInRoleAsync(RolAdministrador);
            administradorLegado.Name = RolGestorCompeticion;
            administradorLegado.NormalizedName = RolGestorCompeticion.ToUpperInvariant();
            await roleManager.UpdateAsync(administradorLegado);

            if (!await roleManager.RoleExistsAsync(RolAdministrador))
            {
                await roleManager.CreateAsync(new IdentityRole(RolAdministrador));
            }

            // Un único administrador legado (caso típico: la cuenta ya sembrada)
            // pasa a tener también el rol de sistema nuevo, sin credenciales nuevas.
            if (usuariosLegado.Count == 1)
            {
                await userManager.AddToRoleAsync(usuariosLegado[0], RolAdministrador);
            }

            return;
        }

        if (!gestorCompeticionExiste)
        {
            await roleManager.CreateAsync(new IdentityRole(RolGestorCompeticion));
        }

        if (!await roleManager.RoleExistsAsync(RolAdministrador))
        {
            await roleManager.CreateAsync(new IdentityRole(RolAdministrador));
        }
    }
}
