var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithHostPort(1433)
    .WithDataVolume();
var db = sql.AddDatabase("basketbasetracker");

builder.AddProject<Projects.BasketBaseTracker_Web>("web")
    .WithReference(db)
    .WaitFor(db);

var app = builder.Build();

await app.RunAsync();
