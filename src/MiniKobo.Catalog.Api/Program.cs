using MiniKobo.Catalog.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRavenDb();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
