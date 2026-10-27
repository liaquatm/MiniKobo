using MiniKobo.Catalog.Api.Data;
using MiniKobo.Catalog.Api.Features.Books;
using MiniKobo.Catalog.Api.Messaging;
using Raven.Client.Documents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddRavenDb();
builder.Services.AddCatalogEndpoint(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    var store = app.Services.GetRequiredService<IDocumentStore>();
    await CatalogSeeder.SeedAsync(store);
}

app.MapBookEndpoints();

app.Run();