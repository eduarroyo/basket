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
        await page.FillAsync("#Input_Email", WebAppFixture.AdminEmail);
        await page.FillAsync("#Input_Password", "ContraseñaIncorrecta123!");
        await page.ClickAsync("button[type=submit]");
        await Assertions.Expect(page.GetByText("Usuario o contraseña incorrectos")).ToBeVisibleAsync();

        // Credenciales del seed: autentica.
        await page.FillAsync("#Input_Email", WebAppFixture.AdminEmail);
        await page.FillAsync("#Input_Password", WebAppFixture.AdminPassword);
        await page.ClickAsync("button[type=submit]");

        // La sesión autenticada se reconoce dentro del Area Admin (Logout, la única
        // página del Area además de Login en este incremento, muestra el botón de
        // cerrar sesión solo si hay un usuario autenticado).
        await page.GotoAsync(new Uri(fixture.WebUrl, "/Admin/Logout").ToString());
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Cerrar sesión" })).ToBeVisibleAsync();

        await page.ClickAsync("button:has-text('Cerrar sesión')");
        await Assertions.Expect(page.GetByText("Has cerrado sesión correctamente")).ToBeVisibleAsync();
    }
}
