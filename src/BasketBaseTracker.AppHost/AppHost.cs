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
}

// Sin registro de contenedores propio: AddAzureContainerAppEnvironment aprovisiona
// siempre su propio Azure Container Registry (Basic) para la identidad administrada
// del entorno, se use o no para las imágenes — GHCR no evita ese coste fijo, así
// que se acepta el ACR por defecto en vez de gestionar un registro adicional
// (architecture.md punto 12, revisado en BAS-3).

var app = builder.Build();

await app.RunAsync();
