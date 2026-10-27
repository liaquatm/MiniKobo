using Microsoft.Extensions.Options;

namespace MiniKobo.Orders.Api.Clients.Catalog;

public static class CatalogServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogClient(this IServiceCollection services)
    {
        services.AddOptions<CatalogOptions>()
            .BindConfiguration(CatalogOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<ICatalogClient, CatalogClient>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<CatalogOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddStandardResilienceHandler();

        return services;
    }
}
