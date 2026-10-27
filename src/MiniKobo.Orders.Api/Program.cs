using Microsoft.EntityFrameworkCore;
using MiniKobo.Orders.Api.Clients.Catalog;
using MiniKobo.Orders.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddDbContext<OrdersDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("Orders")
        ?? throw new InvalidOperationException("Connection string 'Orders' is not configured."))
    .UseSnakeCaseNamingConvention());

builder.Services.AddCatalogClient();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapControllers();

app.Run();
