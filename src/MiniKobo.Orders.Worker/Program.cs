using MiniKobo.Contracts;
using MiniKobo.Orders.Worker;
using Npgsql;
using NpgsqlTypes;
using NServiceBus.Persistence.Sql;

[assembly: SqlPersistenceSettings(MsSqlServerScripts = false, MySqlScripts = false, OracleScripts = false, PostgreSqlScripts = true)]

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));

var endpoint = new EndpointConfiguration("MiniKobo.Orders.Processing");
endpoint.UseSerialization<SystemJsonSerializer>();

var routing = endpoint.UseTransport(new RabbitMQTransport(
    RoutingTopology.Conventional(QueueType.Quorum),
    builder.Configuration.GetConnectionString("RabbitMQ")
        ?? throw new InvalidOperationException("Connection string 'RabbitMQ' is not configured.")));
routing.RouteToEndpoint(typeof(ReserveStock), "MiniKobo.Catalog");
routing.RouteToEndpoint(typeof(ReleaseStock), "MiniKobo.Catalog");

// Saga state and the outbox live in the Orders PostgreSQL database (in their own tables).
var ordersConnectionString = builder.Configuration.GetConnectionString("Orders")
    ?? throw new InvalidOperationException("Connection string 'Orders' is not configured.");
var persistence = endpoint.UsePersistence<SqlPersistence>();
persistence.SqlDialect<SqlDialect.PostgreSql>()
    .JsonBParameterModifier(parameter => ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Jsonb);
persistence.ConnectionBuilder(() => new NpgsqlConnection(ordersConnectionString));
persistence.TablePrefix("nsb_processing_"); // PostgreSQL limits the prefix to 20 characters

endpoint.EnableOutbox();
endpoint.EnableInstallers();

builder.Services.AddNServiceBusEndpoint(endpoint);

builder.Build().Run();
