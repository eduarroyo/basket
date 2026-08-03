var builder = DistributedApplication.CreateBuilder(args);

var app = builder.Build();

await app.RunAsync();
