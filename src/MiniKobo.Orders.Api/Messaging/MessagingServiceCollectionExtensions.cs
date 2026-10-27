using MiniKobo.Orders.Api.Features.Orders;
using Npgsql;
using NpgsqlTypes;
using NServiceBus;
using NServiceBus.Persistence.Sql;

[assembly: SqlPersistenceSettings(MsSqlServerScripts = false, MySqlScripts = false, OracleScripts = false, PostgreSqlScripts = true)]

namespace MiniKobo.Orders.Api.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public const string EndpointName = "MiniKobo.Orders";

    /// <summary>
    /// Makes this API an NServiceBus endpoint: RabbitMQ for messages, PostgreSQL for the outbox.
    /// Turned off with Messaging:Enabled=false (e.g. in HTTP-only integration tests).
    /// </summary>
    public static IServiceCollection AddOrdersEndpoint(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue("Messaging:Enabled", true))
        {
            return services;
        }

        var endpoint = new EndpointConfiguration(EndpointName);
        endpoint.UseSerialization<SystemJsonSerializer>();

        var routing = endpoint.UseTransport(new RabbitMQTransport(
            RoutingTopology.Conventional(QueueType.Quorum),
            configuration.GetConnectionString("RabbitMQ")
                ?? throw new InvalidOperationException("Connection string 'RabbitMQ' is not configured.")));
        routing.RouteToEndpoint(typeof(PlaceOrder), EndpointName);

        var ordersConnectionString = configuration.GetConnectionString("Orders")
            ?? throw new InvalidOperationException("Connection string 'Orders' is not configured.");
        var persistence = endpoint.UsePersistence<SqlPersistence>();
        persistence.SqlDialect<SqlDialect.PostgreSql>()
            .JsonBParameterModifier(parameter => ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Jsonb);
        persistence.ConnectionBuilder(() => new NpgsqlConnection(ordersConnectionString));
        persistence.TablePrefix("nsb_orders_"); // keeps NServiceBus tables apart from our EF tables

        // The outbox stores outgoing messages in the same database transaction as our data,
        // then sends them. It also de-duplicates incoming messages.
        endpoint.EnableOutbox();

        // Creates queues and the outbox tables on startup (fine for development).
        endpoint.EnableInstallers();

        services.AddNServiceBusEndpoint(endpoint);
        return services;
    }
}
