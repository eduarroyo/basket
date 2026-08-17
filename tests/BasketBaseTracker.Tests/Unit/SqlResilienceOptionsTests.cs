using BasketBaseTracker.Web.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BasketBaseTracker.Tests.Unit;

public class SqlResilienceOptionsTests
{
    [Fact]
    public void ValoresPorDefecto_CuandoNoHaySeccionDeConfiguracion()
    {
        var options = Resolve([]);

        Assert.Equal(4, options.MaxRetryCount);
        Assert.Equal(10, options.MaxRetryDelaySeconds);
    }

    [Fact]
    public void LeeLosValoresDeLaSeccionSqlResilience_CuandoEstanConfigurados()
    {
        var options = Resolve(new Dictionary<string, string?>
        {
            ["Sql:Resilience:MaxRetryCount"] = "7",
            ["Sql:Resilience:MaxRetryDelaySeconds"] = "20",
        });

        Assert.Equal(7, options.MaxRetryCount);
        Assert.Equal(20, options.MaxRetryDelaySeconds);
    }

    // Registra SqlResilienceOptions exactamente como Program.cs, para que el test
    // cubra el binding real de configuración usado en producción, no una llamada
    // aparte a IConfiguration.Get<T>().
    private static SqlResilienceOptions Resolve(Dictionary<string, string?> configurationValues)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        var services = new ServiceCollection();
        services.Configure<SqlResilienceOptions>(configuration.GetSection(SqlResilienceOptions.SectionName));

        using var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IOptionsMonitor<SqlResilienceOptions>>().CurrentValue;
    }
}
