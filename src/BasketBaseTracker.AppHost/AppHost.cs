using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql");
if (!builder.Configuration.GetValue<bool>("Sql:Ephemeral"))
{
    // Puerto fijo + volumen persistente: solo para desarrollo local interactivo
    // (aspire run/start), para tener una cadena de conexión estable entre reinicios
    // y poder conectar herramientas externas (dotnet ef database update, SSMS...).
    // Los tests (Aspire.Hosting.Testing) activan "Sql:Ephemeral" para no compartir
    // ni el puerto ni los datos con la base de datos de desarrollo local ni entre sí.
    sql.WithHostPort(1433).WithDataVolume();
}
var db = sql.AddDatabase("basketbasetracker");

builder.AddProject<Projects.BasketBaseTracker_Web>("web")
    .WithReference(db)
    .WaitFor(db);

var app = builder.Build();

await app.RunAsync();
