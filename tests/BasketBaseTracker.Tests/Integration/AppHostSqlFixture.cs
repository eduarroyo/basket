using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BasketBaseTracker.Tests.Integration;

// Arranca el AppHost completo (Web + contenedor SQL Server efímero) una sola vez
// para toda la clase de test que lo use (IClassFixture<AppHostSqlFixture>), en vez
// de por cada [Fact] — cada arranque tarda cerca de un minuto (BAS-5, plan.md).
//
// La espera de arranque usa una petición HTTP a /health con reintentos (igual que
// AppHostTests.WebRespondeAlHealthCheck), no ResourceNotifications.WaitForResourceHealthyAsync:
// el recurso "web" no tiene health checks propios configurados, así que Aspire lo
// da por "sano" casi al instante, mucho antes de que el proceso arranque de verdad
// (comprobado: los tests llegaban a ejecutarse contra una base de datos sin
// migrar). Una respuesta 2xx de /health sí garantiza que la migración ya se aplicó,
// porque Program.cs llama a Database.MigrateAsync() antes de que Kestrel empiece a
// escuchar.
public class AppHostSqlFixture : IAsyncLifetime
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(3);

    private DistributedApplication? _app;

    public string ConnectionString { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.BasketBaseTracker_AppHost>(["--Sql:Ephemeral=true"], cancellationToken);

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        appHost.CreateResourceBuilder<ProjectResource>("web")
            .WithEnvironment("Seed__AdminEmail", "test-admin@basketbasetracker.local")
            .WithEnvironment("Seed__AdminPassword", "ClaveDePruebasDeIntegracion123!");

        _app = await appHost.BuildAsync(cancellationToken).WaitAsync(Timeout, cancellationToken);
        await _app.StartAsync(cancellationToken).WaitAsync(Timeout, cancellationToken);

        using (var httpClient = _app.CreateHttpClient("web"))
        {
            using var response = await httpClient.GetAsync("/health", cancellationToken)
                .WaitAsync(Timeout, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        ConnectionString = await _app.GetConnectionStringAsync("basketbasetracker", cancellationToken)
            ?? throw new InvalidOperationException(
                "No se pudo obtener la cadena de conexión del recurso 'basketbasetracker'.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}
