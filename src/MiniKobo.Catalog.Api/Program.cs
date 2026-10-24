using MiniKobo.Catalog.Api.Data;
using MiniKobo.Catalog.Api.Features.Books;
using Raven.Client.Documents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRavenDb();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var store = app.Services.GetRequiredService<IDocumentStore>();
    await CatalogSeeder.SeedAsync(store);
}

app.MapBookEndpoints();

app.Run();