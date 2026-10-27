using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MiniKobo.Orders.Api.Clients.Catalog;
using MiniKobo.Orders.Api.Data;
using NServiceBus;
using NServiceBus.Testing;
using Testcontainers.PostgreSql;

namespace MiniKobo.Orders.Api.IntegrationTests;

/// <summary>
/// Runs the real Orders API in memory against a throwaway PostgreSQL container.
/// Catalog and the message bus are replaced with fakes, so only Orders is under test.
/// </summary>
public sealed class OrdersApiFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();

    public FakeCatalogClient Catalog { get; } = new();

    // Records every message the API sends, instead of putting it on RabbitMQ.
    public TestableMessageSession MessageSession { get; } = new();

    public Task StartContainerAsync() => _postgres.StartAsync();

    /// <summary>Applies the same migrations as development to the empty test database.</summary>
    public async Task MigrateAsync()
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Orders", _postgres.GetConnectionString());
        builder.UseSetting("Messaging:Enabled", "false");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICatalogClient>();
            services.AddSingleton<ICatalogClient>(Catalog);
            services.AddSingleton<IMessageSession>(MessageSession);
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
