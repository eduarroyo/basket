using Aspire.Hosting.Pipelines;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

#pragma warning disable ASPIREPIPELINES001 // Pipeline steps (builder.Pipeline.AddStep) es experimental en Aspire 13.4.

const string SqlAdminLoginName = "sqladmin";
const string SqlAppLoginName = "basketbasetracker_app";
// Nombre fijo (en vez del take('sql-${uniqueString(...)}', 63) por defecto de
// Aspire) para poder construir el FQDN como literal en AppHost.cs sin resolver
// ningún output del recurso "sql" en tiempo de ejecución — referenciar
// sql.Resource.FullyQualifiedDomainName (vía ReferenceExpression o
// GetValueAsync) dispara el script de asignación de roles roto (ver más abajo)
// y, en el caso de GetValueAsync dentro de un callback, se queda colgado en
// "aspire deploy --list-steps" esperando un output que nunca llega porque no
// se ha aprovisionado nada todavía.
const string SqlServerName = "basketbasetracker-sql";

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aca-env");

// Solo se usan en modo publish (autenticación SQL de producción, ver más abajo);
// declararlos aquí y no dentro del bloque IsPublishMode evita problemas de orden
// de captura en los closures de ConfigureInfrastructure/Pipeline.AddStep. No piden
// valor en local porque nada los resuelve fuera de publish mode.
var sqlAdminPassword = builder.AddParameter("sql-admin-password", secret: true);
var sqlAppPassword = builder.AddParameter("sql-app-password", secret: true);

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
    })
    .ConfigureInfrastructure(infra =>
    {
        // La oferta gratuita de Azure SQL (useFreeLimit, activada por defecto por
        // AddDatabase) no está soportada en spaincentral para esta suscripción
        // (ProvisioningDisabled al desplegar). Se desactiva explícitamente y se fija
        // el SKU Serverless (mismo que el valor por defecto), preservando el tier
        // Serverless con auto-pause de architecture.md punto 3, solo sin el
        // descuento gratuito — WithDefaultAzureSku() no sirve aquí porque quita el
        // SKU serverless entero, no solo la oferta gratuita.
        var database = infra.GetProvisionableResources().OfType<SqlDatabase>().Single();
        database.Sku = new SqlSku { Name = "GP_S_Gen5_2" };
        database.UseFreeLimit = false;

        if (builder.ExecutionContext.IsPublishMode)
        {
            // Autenticación SQL en vez de solo Managed Identity (architecture.md punto 6,
            // revisado en BAS-3): el script que Aspire genera para dar de alta la identidad
            // administrada de Web como usuario de la base de datos falla de forma
            // determinista en este entorno (MissingMethodException del módulo de
            // PowerShell SqlServer al registrar el proveedor Always Encrypted de Key
            // Vault — ver spec.md, "¿Se puede seguir usando Managed Identity para Azure
            // SQL?"). Se mantiene el administrador de Microsoft Entra que Aspire configura
            // por defecto (inofensivo, sin coste) y se añade además un login SQL admin con
            // contraseña (parámetro secreto de Aspire, expuesto como secreto del propio
            // Container App, no en claro), usado solo por el paso de pipeline
            // "provision-sql-app-login" de abajo para crear el login de la propia
            // aplicación con privilegios mínimos — Web nunca se conecta con el admin.
            var sqlServer = infra.GetProvisionableResources().OfType<SqlServer>().Single();
            sqlServer.Name = SqlServerName;
            sqlServer.AdministratorLogin = SqlAdminLoginName;
            sqlServer.AdministratorLoginPassword = sqlAdminPassword.AsProvisioningParameter(infra);
            sqlServer.Administrators.IsAzureADOnlyAuthenticationEnabled = false;
        }
    });
var db = sql.AddDatabase("basketbasetracker");

var web = builder.AddProject<Projects.BasketBaseTracker_Web>("web")
    .WithExternalHttpEndpoints();

if (builder.ExecutionContext.IsPublishMode)
{
    // Modo Multiple revisiones (deploy.yml, tarea 25 de BAS-3): el Bicep que genera
    // Aspire por defecto deja "Single", que sustituye toda la revisión activa (y su
    // tráfico) en cada despliegue, sin ventana para el smoke test antes de
    // promocionar. Fijarlo aquí (en vez de solo con "az containerapp revision
    // set-mode" a mano, como se hizo la primera vez) evita que el próximo "aspire
    // deploy" manual lo revierta sin querer — spec.md documenta el porqué.
    web.PublishAsAzureContainerApp((infra, containerApp) =>
    {
        containerApp.Configuration.ActiveRevisionsMode = ContainerAppActiveRevisionsMode.Multiple;
        containerApp.Template.Scale.MinReplicas = 1;
        containerApp.Template.Scale.MaxReplicas = 2;
    });
}

if (builder.ExecutionContext.IsPublishMode)
{
    // Sin emulador ni contenedor para Azure Key Vault (spec.md de BAS-3): en local
    // se sigue sin Key Vault, con dotnet user-secrets tal cual. Solo se aprovisiona
    // y se referencia desde Web al desplegar (aspire deploy).
    var kv = builder.AddAzureKeyVault("kv");
    web.WithReference(kv);

    // Application Insights (tarea 26/27 de BAS-3): solo en modo publish, igual que
    // Key Vault — en local, "aspire run" ya da logs/trazas/métricas OTLP en el
    // dashboard sin necesidad de un recurso Azure. WithReference inyecta
    // APPLICATIONINSIGHTS_CONNECTION_STRING en Web (ServiceDefaults/Extensions.cs
    // activa el exportador de Azure Monitor solo si esa variable existe).
    var appInsights = builder.AddAzureApplicationInsights("app-insights");
    web.WithReference(appInsights);

    // Credenciales del *seed* del primer administrador (architecture.md punto 7,
    // tasks.md de BAS-3): en Key Vault, no en variables de entorno en claro. Web las
    // lee vía AddAzureKeyVaultSecrets("kv") en Program.cs (IdentitySeeder ya las
    // busca en IConfiguration como "Seed:AdminEmail"/"Seed:AdminPassword" — el
    // proveedor de Key Vault traduce "--" a ":" automáticamente).
    var seedAdminEmail = builder.AddParameter("seed-admin-email", "eduarroyo@gmail.com");
    var seedAdminPassword = builder.AddParameter("seed-admin-password", secret: true);
    kv.AddSecret("kv-seed-admin-email", "Seed--AdminEmail", seedAdminEmail);
    kv.AddSecret("kv-seed-admin-password", "Seed--AdminPassword", seedAdminPassword);

    var sqlServerFqdn = $"{SqlServerName}.database.windows.net";

    // No se usa WithReference(db) ni sql.Resource.FullyQualifiedDomainName: ambos
    // registran una referencia al recurso "sql" en el grafo de Aspire, lo que
    // dispara el script de asignación de roles roto (comprobado con aspire deploy
    // --list-steps), aunque Web nunca use la identidad administrada para conectar.
    // Al usar el FQDN determinista (SqlServerName es fijo) esta cadena de conexión
    // es un literal — sqlAppPassword sí se referencia, pero referenciar un
    // parámetro no dispara asignación de roles, solo referenciar el recurso Azure.
    web.WithEnvironment(
        "ConnectionStrings__basketbasetracker",
        ReferenceExpression.Create(
            $"Server=tcp:{sqlServerFqdn},1433;Initial Catalog=basketbasetracker;User ID={SqlAppLoginName};Password={sqlAppPassword.Resource};Encrypt=True;TrustServerCertificate=False;"));

    // Paso de pipeline propio (en vez del script de Aspire, que falla de forma
    // determinista con Key Vault presente — ver más arriba): crea, de forma
    // idempotente, el login de aplicación como usuario contenido de la base de
    // datos, con permisos mínimos (lectura/escritura, sin DDL). Usa
    // Microsoft.Data.SqlClient directamente, no el módulo de PowerShell SqlServer.
    builder.Pipeline.AddStep(
        "provision-sql-app-login",
        async context =>
        {
            var adminPassword = await sqlAdminPassword.Resource.GetValueAsync(context.CancellationToken);
            var appPassword = await sqlAppPassword.Resource.GetValueAsync(context.CancellationToken);

            var adminConnectionString =
                $"Server=tcp:{sqlServerFqdn},1433;Initial Catalog=basketbasetracker;User ID={SqlAdminLoginName};Password={adminPassword};Encrypt=True;TrustServerCertificate=False;";

            await using var connection = new SqlConnection(adminConnectionString);
            await connection.OpenAsync(context.CancellationToken);

            await using var command = connection.CreateCommand();
            // Comillas simples duplicadas: escapado estándar de literales T-SQL
            // (la contraseña la genera Aspire, no es entrada de usuario, pero se
            // escapa igualmente por si contiene alguna).
            var escapedAppPassword = appPassword?.Replace("'", "''");
            command.CommandText = $"""
                IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '{SqlAppLoginName}')
                BEGIN
                    CREATE USER [{SqlAppLoginName}] WITH PASSWORD = '{escapedAppPassword}';
                    ALTER ROLE db_datareader ADD MEMBER [{SqlAppLoginName}];
                    ALTER ROLE db_datawriter ADD MEMBER [{SqlAppLoginName}];
                    GRANT EXECUTE TO [{SqlAppLoginName}];
                END
                """;
            await command.ExecuteNonQueryAsync(context.CancellationToken);
        },
        // requiredBy apunta al paso "deploy" (siempre existe desde el arranque), no a
        // "provision-web-containerapp": referenciar un paso contribuido por un recurso
        // (como ese) desde requiredBy falla con "unknown step" — se valida antes de que
        // ese paso quede registrado. dependsOn sí funciona con pasos de recursos.
        dependsOn: "provision-sql",
        requiredBy: WellKnownPipelineSteps.Deploy);
}
else
{
    web.WithReference(db).WaitFor(db);
}

// Sin registro de contenedores propio: AddAzureContainerAppEnvironment aprovisiona
// siempre su propio Azure Container Registry (Basic) para la identidad administrada
// del entorno, se use o no para las imágenes — GHCR no evita ese coste fijo, así
// que se acepta el ACR por defecto en vez de gestionar un registro adicional
// (architecture.md punto 12, revisado en BAS-3).

var app = builder.Build();

await app.RunAsync();
