using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BasketBaseTracker.Tests.Integration;

public class AppHostTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(3);

    [Fact]
    public async Task WebRespondeAlHealthCheck()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // "Sql:Ephemeral" hace que el AppHost use un contenedor de SQL Server sin
        // puerto fijo ni volumen persistente (AppHost.cs), para no compartir estado
        // ni puerto con la base de datos de desarrollo local ni con otros tests.
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.BasketBaseTracker_AppHost>(["--Sql:Ephemeral=true"], cancellationToken);

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        // El seed del administrador (Program.cs) exige credenciales en Development.
        // Este test solo comprueba el health check, así que valen unas de prueba.
        appHost.CreateResourceBuilder<ProjectResource>("web")
            .WithEnvironment("Seed__AdminEmail", "test-admin@basketbasetracker.local")
            .WithEnvironment("Seed__AdminPassword", "ClaveDePruebasDeIntegracion123!");

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(Timeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(Timeout, cancellationToken);

        await app.ResourceNotifications.WaitForResourceHealthyAsync("web", cancellationToken)
            .WaitAsync(Timeout, cancellationToken);

        using var httpClient = app.CreateHttpClient("web");
        using var response = await httpClient.GetAsync("/health", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
