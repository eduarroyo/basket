using Microsoft.Playwright;

namespace BasketBaseTracker.Tests.E2E;

[Collection(WebAppCollection.Name)]
public class LoginTests(WebAppFixture fixture)
{
    [Fact]
    public async Task AdministradorSeededPuedeIniciarSesionYCerrarla()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();

        var loginUrl = new Uri(fixture.WebUrl, "/Admin/Login").ToString();

        // Credenciales incorrectas: no autentica.
        await page.GotoAsync(loginUrl);
        await page.FillAsync("#Input_Email", fixture.AdminEmail);
        await page.FillAsync("#Input_Password", "ContraseñaIncorrecta123!");
        await page.ClickAsync("button[type=submit]");
        await Assertions.Expect(page.GetByText("Usuario o contraseña incorrectos")).ToBeVisibleAsync();

        // Credenciales del seed: autentica y aterriza en el panel de administración
        // (BAS-6), cuyo menú muestra "Cerrar sesión" en todas las páginas autenticadas.
        await page.FillAsync("#Input_Email", fixture.AdminEmail);
        await page.FillAsync("#Input_Password", fixture.AdminPassword);
        await page.ClickAsync("button[type=submit]");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Panel de administración" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Cerrar sesión" })).ToBeVisibleAsync();

        // Tras cerrar sesión, vuelve a la Portada (Logout.cshtml.cs).
        await page.ClickAsync("button:has-text('Cerrar sesión')");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "BasketBaseTracker" })).ToBeVisibleAsync();

        await page.GotoAsync(new Uri(fixture.WebUrl, "/Admin/Logout").ToString());
        await Assertions.Expect(page.GetByText("Has cerrado sesión correctamente")).ToBeVisibleAsync();
    }
}
