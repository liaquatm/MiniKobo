using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MiniKobo.Orders.Api.Clients.Catalog;
using MiniKobo.Orders.Api.Data;
using MiniKobo.Orders.Api.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddDbContext<OrdersDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("Orders")
        ?? throw new InvalidOperationException("Connection string 'Orders' is not configured."))
    .UseSnakeCaseNamingConvention());

builder.Services.AddCatalogClient();
builder.Services.AddOrdersEndpoint(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapControllers();

app.Run();
