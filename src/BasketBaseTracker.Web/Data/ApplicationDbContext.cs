using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BasketBaseTracker.Web.Data;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IOptionsMonitor<SqlResilienceOptions> sqlResilienceOptions) : IdentityDbContext(options)
{
    // OnConfiguring se ejecuta en cada instancia nueva del contexto (una por
    // petición HTTP, dado el ciclo de vida Scoped por defecto), así que
    // IOptionsMonitor siempre entrega el valor vigente de configuración — permite
    // ajustar los reintentos sin reiniciar la app en local (appsettings.Development.json
    // se recarga en caliente); en producción, una variable de entorno en Container
    // Apps no se recarga en caliente y requiere una nueva revisión (sin rebuild de
    // imagen), ver README.md.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var resilience = sqlResilienceOptions.CurrentValue;
        optionsBuilder.UseSqlServer(sqlServerOptions =>
            sqlServerOptions.EnableRetryOnFailure(
                resilience.MaxRetryCount,
                TimeSpan.FromSeconds(resilience.MaxRetryDelaySeconds),
                errorNumbersToAdd: null));
    }
}
