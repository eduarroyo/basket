using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BasketBaseTracker.Tests.E2E;

// Arranca el AppHost una sola vez para todos los tests E2E de la colección (WebAppCollection)
// en vez de por test — levantar el contenedor de SQL Server tarda del orden de segundos.
public sealed class WebAppFixture : IAsyncLifetime
{
    private const string DefaultAdminEmail = "test-admin@basketbasetracker.local";
    private const string DefaultAdminPassword = "ClaveDePruebasE2E123!";

    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(3);

    private DistributedApplication? _app;

    public Uri WebUrl { get; private set; } = null!;
    public string AdminEmail { get; private set; } = DefaultAdminEmail;
    public string AdminPassword { get; private set; } = DefaultAdminPassword;

    public async ValueTask InitializeAsync()
    {
        // Modo "URL externa" (deploy.yml, BAS-3): smoke test contra la revisión de
        // Container Apps recién desplegada en vez de levantar un AppHost local — no
        // hay seed de prueba ahí, así que las credenciales del admin también vienen
        // de fuera (el admin real, ya sembrado en producción).
        var externalUrl = Environment.GetEnvironmentVariable("E2E_EXTERNAL_URL");
        if (!string.IsNullOrEmpty(externalUrl))
        {
            WebUrl = new Uri(externalUrl);
            AdminEmail = Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL")
                ?? throw new InvalidOperationException("E2E_ADMIN_EMAIL es obligatorio cuando se define E2E_EXTERNAL_URL.");
            AdminPassword = Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD")
                ?? throw new InvalidOperationException("E2E_ADMIN_PASSWORD es obligatorio cuando se define E2E_EXTERNAL_URL.");
            return;
        }

        // "Sql:Ephemeral" hace que el AppHost use un contenedor de SQL Server sin
        // puerto fijo ni volumen persistente (AppHost.cs), para no compartir estado
        // ni puerto con la base de datos de desarrollo local ni con otros tests —
        // si no, el seed idempotente del admin (IdentitySeeder) no crea AdminEmail/
        // AdminPassword porque ya existe un administrador de una ejecución anterior.
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.BasketBaseTracker_AppHost>(["--Sql:Ephemeral=true"]);

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        // El seed del administrador (Program.cs) exige credenciales en Development.
        appHost.CreateResourceBuilder<ProjectResource>("web")
            .WithEnvironment("Seed__AdminEmail", AdminEmail)
            .WithEnvironment("Seed__AdminPassword", AdminPassword);

        _app = await appHost.BuildAsync().WaitAsync(Timeout);
        await _app.StartAsync().WaitAsync(Timeout);

        await _app.ResourceNotifications.WaitForResourceHealthyAsync("web").WaitAsync(Timeout);

        WebUrl = _app.GetEndpoint("web");
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class WebAppCollection : ICollectionFixture<WebAppFixture>
{
    public const string Name = "WebApp";
}
