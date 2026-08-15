namespace BasketBaseTracker.Web.Data;

// Reintentos ante fallos transitorios de Azure SQL (auto-resume del tier
// Serverless, throttling, failover) — ver docs/specs/BAS-3/spec.md, aclaración
// sobre EnableRetryOnFailure. Los valores por defecto están dimensionados para
// no acercarse al timeout de ingress de Azure Container Apps (240s).
public class SqlResilienceOptions
{
    public const string SectionName = "Sql:Resilience";

    public int MaxRetryCount { get; set; } = 4;

    public int MaxRetryDelaySeconds { get; set; } = 10;
}
