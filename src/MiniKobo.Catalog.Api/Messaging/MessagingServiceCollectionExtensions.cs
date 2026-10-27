using NServiceBus;

namespace MiniKobo.Catalog.Api.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public const string EndpointName = "MiniKobo.Catalog";

    /// <summary>
    /// Makes this API an NServiceBus endpoint that receives stock commands from RabbitMQ.
    /// Turned off with Messaging:Enabled=false (e.g. in HTTP-only integration tests).
    /// </summary>
    public static IServiceCollection AddCatalogEndpoint(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue("Messaging:Enabled", true))
        {
            return services;
        }

        var endpoint = new EndpointConfiguration(EndpointName);
        endpoint.UseSerialization<SystemJsonSerializer>();
        endpoint.UseTransport(new RabbitMQTransport(
            RoutingTopology.Conventional(QueueType.Quorum),
            configuration.GetConnectionString("RabbitMQ")
                ?? throw new InvalidOperationException("Connection string 'RabbitMQ' is not configured.")));

        // Creates this endpoint's queues on startup (fine for development; production uses scripts).
        endpoint.EnableInstallers();

        services.AddNServiceBusEndpoint(endpoint);
        return services;
    }
}
