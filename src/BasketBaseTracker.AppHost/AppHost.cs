using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aca-env");

// AddAzureSqlServer + RunAsContainer: en local (aspire run/start, tests) sigue
// lanzando un contenedor SQL Server igual que antes con AddSqlServer, sin coste
// ni base de datos Azure adicional; Azure SQL Database Serverless real solo se
// aprovisiona al desplegar (aspire deploy) — ver spec.md de BAS-3.
var sql = builder.AddAzureSqlServer("sql")
    .RunAsContainer(container =>
    {
        if (!builder.Configuration.GetValue<bool>("Sql:Ephemeral"))
        {
            // Puerto fijo + volumen persistente: solo para desarrollo local interactivo
            // (aspire run/start), para tener una cadena de conexión estable entre reinicios
            // y poder conectar herramientas externas (dotnet ef database update, SSMS...).
            // Los tests (Aspire.Hosting.Testing) activan "Sql:Ephemeral" para no compartir
            // ni el puerto ni los datos con la base de datos de desarrollo local ni entre sí.
            container.WithHostPort(1433).WithDataVolume();
        }
    });
var db = sql.AddDatabase("basketbasetracker");

var web = builder.AddProject<Projects.BasketBaseTracker_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(db)
    .WaitFor(db);

if (builder.ExecutionContext.IsPublishMode)
{
    // Sin emulador ni contenedor para Azure Key Vault (spec.md de BAS-3): en local
    // se sigue sin Key Vault, con dotnet user-secrets tal cual. Solo se aprovisiona
    // y se referencia desde Web al desplegar (aspire deploy).
    var kv = builder.AddAzureKeyVault("kv");
    web.WithReference(kv);

    // GitHub Container Registry en vez del Azure Container Registry por defecto de
    // azd (architecture.md punto 12) — evita su coste fijo. API experimental
    // (ASPIRECOMPUTE003): revisar en cada actualización de Aspire si sigue disponible.
#pragma warning disable ASPIRECOMPUTE003
    var registry = builder.AddContainerRegistry("ghcr", "ghcr.io", "eduarroyo/basket");
    web.WithContainerRegistry(registry);
#pragma warning restore ASPIRECOMPUTE003
}

var app = builder.Build();

await app.RunAsync();
