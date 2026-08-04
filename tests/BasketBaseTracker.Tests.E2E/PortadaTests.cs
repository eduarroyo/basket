using Microsoft.Playwright;

namespace BasketBaseTracker.Tests.E2E;

[Collection(WebAppCollection.Name)]
public class PortadaTests(WebAppFixture fixture)
{
    [Fact]
    public async Task UsuarioAnonimoPuedeVerLaPortada()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();

        await page.GotoAsync(fixture.WebUrl.ToString());

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "BasketBaseTracker" }))
            .ToBeVisibleAsync();
    }
}
