using System.Net;
using System.Text;
using BasketBaseTracker.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static BasketBaseTracker.Tests.Integration.AdminHttpTestHelpers;

namespace BasketBaseTracker.Tests.Integration;

// Cubre la pantalla ImportExport de BAS-16: versión no soportada y checkbox sin
// marcar no cambian nada, y el control de acceso por rol (solo "Administrador",
// no "GestorCompeticion"). El reemplazo completo real vive aparte
// (ImportExportReemplazoCompletoTests) — es destructivo para toda la base de
// datos y no puede convivir con estos tests si xUnit los ejecuta en paralelo
// dentro de la misma clase (mismo motivo ya documentado en AdminHttpTestHelpers).
public class ImportExportAdminPagesTests(AppHostSqlFixture fixture) : IClassFixture<AppHostSqlFixture>
{
    private static ServiceProvider ConstruirProveedorIdentity(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddIdentityCore<IdentityUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        return services.BuildServiceProvider();
    }

    // Crea un usuario de prueba con exactamente los roles indicados (sin tocar la
    // cuenta sembrada compartida) e inicia sesión con él en un HttpClient nuevo.
    private static async Task<HttpClient> CrearClienteConRolesAsync(
        AppHostSqlFixture fixture, string sufijo, string[] roles, CancellationToken cancellationToken)
    {
        await using var provider = ConstruirProveedorIdentity(fixture.ConnectionString);
        using (var scope = provider.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var email = $"test-{sufijo}@basketbasetracker.local";
            const string password = "ClaveDePruebasDeIntegracion123!";
            var usuario = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var resultadoAlta = await userManager.CreateAsync(usuario, password);
            Assert.True(resultadoAlta.Succeeded, string.Join("; ", resultadoAlta.Errors.Select(e => e.Description)));
            var resultadoRoles = await userManager.AddToRolesAsync(usuario, roles);
            Assert.True(resultadoRoles.Succeeded, string.Join("; ", resultadoRoles.Errors.Select(e => e.Description)));
        }

        var client = fixture.CreateIsolatedWebHttpClient();
        var loginPage = await client.GetAsync("/Admin/Login", cancellationToken);
        var token = await GetAntiforgeryTokenAsync(loginPage, cancellationToken);
        using var response = await client.PostAsync(
            "/Admin/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Input.Email"] = $"test-{sufijo}@basketbasetracker.local",
                ["Input.Password"] = "ClaveDePruebasDeIntegracion123!",
            }),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return client;
    }

    [Fact]
    public async Task ImportarConVersionNoSoportadaNoCambiaNada()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var exportacionPrevia = await (await client.GetAsync("/Admin/ImportExport?handler=Exportar", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        var ficheroConVersionMala = """{"SchemaVersion": 99, "Sedes": [], "Clubes": [], "Temporadas": [], "Categorias": [], "Competiciones": [], "Equipos": [], "FichasJugador": [], "Jornadas": [], "Partidos": [], "PartidoParciales": [], "Penalizaciones": []}""";

        var editPage = await client.GetAsync("/Admin/ImportExport", cancellationToken);
        var token = await GetAntiforgeryTokenAsync(editPage, cancellationToken);
        using var content = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent("true"), "ConfirmoReemplazo" },
            { new ByteArrayContent(Encoding.UTF8.GetBytes(ficheroConVersionMala)), "Fichero", "export.json" },
        };
        using var respuesta = await client.PostAsync("/Admin/ImportExport?handler=Importar", content, cancellationToken);
        var html = await respuesta.Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("esquema no soportada", html);

        var exportacionPosterior = await (await client.GetAsync("/Admin/ImportExport?handler=Exportar", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);
        Assert.Equal(exportacionPrevia, exportacionPosterior);
    }

    [Fact]
    public async Task ImportarSinConfirmarElReemplazoNoCambiaNada()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var exportacionPrevia = await (await client.GetAsync("/Admin/ImportExport?handler=Exportar", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        var editPage = await client.GetAsync("/Admin/ImportExport", cancellationToken);
        var token = await GetAntiforgeryTokenAsync(editPage, cancellationToken);
        using var content = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new ByteArrayContent(Encoding.UTF8.GetBytes(exportacionPrevia)), "Fichero", "export.json" },
        };
        using var respuesta = await client.PostAsync("/Admin/ImportExport?handler=Importar", content, cancellationToken);
        var html = await respuesta.Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("Debes confirmar", html);

        var exportacionPosterior = await (await client.GetAsync("/Admin/ImportExport?handler=Exportar", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);
        Assert.Equal(exportacionPrevia, exportacionPosterior);
    }

    [Fact]
    public async Task SoloElRolAdministradorAccedeAImportExport()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        using var clienteSoloGestor = await CrearClienteConRolesAsync(fixture, "solo-gestor", ["GestorCompeticion"], cancellationToken);
        using var respuestaGestor = await clienteSoloGestor.GetAsync("/Admin/ImportExport", cancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, respuestaGestor.StatusCode);

        // El resto del área Admin sigue accesible para GestorCompeticion.
        using var respuestaSedeGestor = await clienteSoloGestor.GetAsync("/Admin/Sede", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, respuestaSedeGestor.StatusCode);

        using var clienteSoloAdministrador = await CrearClienteConRolesAsync(fixture, "solo-admin", ["Administrador"], cancellationToken);
        using var respuestaAdmin = await clienteSoloAdministrador.GetAsync("/Admin/ImportExport", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, respuestaAdmin.StatusCode);

        // Pero no el resto del área Admin, que exige GestorCompeticion.
        using var respuestaSedeAdmin = await clienteSoloAdministrador.GetAsync("/Admin/Sede", cancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, respuestaSedeAdmin.StatusCode);
    }
}
