using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace BasketBaseTracker.Tests.E2E;

public class PortadaTests
{
    private static readonly TimeSpan _timeout = TimeSpan.FromMinutes(3);

    [Fact]
    public async Task UsuarioAnonimoPuedeVerLaPortada()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.BasketBaseTracker_AppHost>(cancellationToken);

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        // El seed del administrador (Program.cs) exige credenciales en Development.
        // Este test solo comprueba la Portada pública, así que valen unas de prueba.
        appHost.CreateResourceBuilder<ProjectResource>("web")
            .WithEnvironment("Seed__AdminEmail", "test-admin@basketbasetracker.local")
            .WithEnvironment("Seed__AdminPassword", "ClaveDePruebasE2E123!");

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(_timeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(_timeout, cancellationToken);

        await app.ResourceNotifications.WaitForResourceHealthyAsync("web", cancellationToken)
            .WaitAsync(_timeout, cancellationToken);

        var webUrl = app.GetEndpoint("web");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();

        await page.GotoAsync(webUrl.ToString());

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "BasketBaseTracker" }))
            .ToBeVisibleAsync();
    }
}
