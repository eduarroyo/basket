var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.BasketBaseTracker_Web>("web");

var app = builder.Build();

await app.RunAsync();
