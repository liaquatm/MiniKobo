using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Raven.Client.Documents.Indexes;

namespace MiniKobo.Catalog.Api.Data;

public static class RavenDbServiceCollectionExtensions
{
    public static IServiceCollection AddRavenDb(this IServiceCollection services)
    {
        services.AddOptions<RavenDbOptions>()
            .BindConfiguration(RavenDbOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IDocumentStore>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<RavenDbOptions>>().Value;

            var store = new DocumentStore
            {
                Urls = options.Urls,
                Database = options.Database
            };
            store.Initialize();
            EnsureDatabaseExists(store);
            IndexCreation.CreateIndexes(typeof(RavenDbServiceCollectionExtensions).Assembly, store);

            return store;
        });

        services.AddScoped(serviceProvider =>
            serviceProvider.GetRequiredService<IDocumentStore>().OpenAsyncSession());

        return services;
    }

    private static void EnsureDatabaseExists(IDocumentStore store)
    {
        try
        {
            store.Maintenance.ForDatabase(store.Database).Send(new GetStatisticsOperation());
        }
        catch (DatabaseDoesNotExistException)
        {
            store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(store.Database)));
        }
    }
}