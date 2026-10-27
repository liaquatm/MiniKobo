using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Testcontainers.RavenDb;

namespace MiniKobo.Catalog.Api.IntegrationTests;

/// <summary>
/// Runs the real Catalog API in memory, pointed at a throwaway RavenDB container.
/// </summary>
public sealed class CatalogApiFactory : WebApplicationFactory<Program>
{
    // Same image as docker-compose.yml, so tests run against the version we develop with.
    private readonly RavenDbContainer _ravenDb = new RavenDbBuilder("ravendb/ravendb:7.1-latest").Build();

    public Task StartContainerAsync() => _ravenDb.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Overrides appsettings.json, exactly like an environment variable would.
        builder.UseSetting("RavenDb:Urls:0", _ravenDb.GetConnectionString());
        builder.UseSetting("RavenDb:Database", "MiniKobo.Catalog.Tests");
        builder.UseSetting("Messaging:Enabled", "false"); // these tests cover HTTP only
    }

    /// <summary>
    /// Queries read from indexes that update in the background, so tests wait until they are up to date.
    /// </summary>
    public async Task WaitForIndexingAsync()
    {
        var store = Services.GetRequiredService<IDocumentStore>();
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var statistics = await store.Maintenance.SendAsync(new GetStatisticsOperation());
            if (statistics.StaleIndexes.Length == 0)
            {
                return;
            }

            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Indexes still stale: {string.Join(", ", statistics.StaleIndexes)}");
            }

            await Task.Delay(100);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _ravenDb.DisposeAsync();
    }
}
