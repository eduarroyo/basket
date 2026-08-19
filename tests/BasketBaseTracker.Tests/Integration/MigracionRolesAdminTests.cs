using BasketBaseTracker.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static BasketBaseTracker.Tests.Integration.AdminHttpTestHelpers;

namespace BasketBaseTracker.Tests.Integration;

// Cubre la migración de roles de BAS-16 (Administrador -> GestorCompeticion +
// Administrador nuevo). Clase dedicada, sin otros [Fact] compartiendo la fixture:
// el test manipula directamente los roles de la cuenta sembrada, así que no puede
// convivir con otros tests que dependan de que esa cuenta siga intacta si xUnit
// los ejecuta en paralelo (mismo motivo documentado en AdminHttpTestHelpers).
public class MigracionRolesAdminTests(AppHostSqlFixture fixture) : IClassFixture<AppHostSqlFixture>
{
    private static ServiceProvider ConstruirProveedorIdentity(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddIdentityCore<IdentityUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task MigraElRolLegadoConservandoElUsuarioYCreaElRolDeSistema()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arranca el AppHost (y con él, el seed normal: GestorCompeticion +
        // Administrador ya creados con ambos roles para la cuenta de pruebas).
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        await using var provider = ConstruirProveedorIdentity(fixture.ConnectionString);
        using var scope = provider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var usuario = await userManager.FindByEmailAsync(AppHostSqlFixture.SeedAdminEmail);
        Assert.NotNull(usuario);

        // Fabrica el estado "legado" previo a BAS-16: deshace el seed ya aplicado y
        // deja solo un rol "Administrador" (sin GestorCompeticion) con el usuario.
        await userManager.RemoveFromRolesAsync(usuario, ["GestorCompeticion", "Administrador"]);
        foreach (var nombreRol in new[] { "GestorCompeticion", "Administrador" })
        {
            var rol = await roleManager.FindByNameAsync(nombreRol);
            if (rol is not null)
            {
                await roleManager.DeleteAsync(rol);
            }
        }

        var rolLegado = new IdentityRole("Administrador");
        await roleManager.CreateAsync(rolLegado);
        await userManager.AddToRoleAsync(usuario, "Administrador");
        var idRolLegado = rolLegado.Id;

        // Ejecuta la migración de nuevo, como si fuera un arranque de contenedor
        // sobre una base de datos con el rol legado todavía sin dividir.
        await IdentitySeeder.MigrarRolesAsync(roleManager, userManager);

        var gestorCompeticion = await roleManager.FindByNameAsync("GestorCompeticion");
        var administradorNuevo = await roleManager.FindByNameAsync("Administrador");

        Assert.NotNull(gestorCompeticion);
        Assert.NotNull(administradorNuevo);
        Assert.Equal(idRolLegado, gestorCompeticion.Id); // renombrado en sitio, no borrado+creado
        Assert.NotEqual(idRolLegado, administradorNuevo.Id); // rol de sistema realmente nuevo
        Assert.True(await userManager.IsInRoleAsync(usuario, "GestorCompeticion"));
        Assert.True(await userManager.IsInRoleAsync(usuario, "Administrador"));
    }
}
