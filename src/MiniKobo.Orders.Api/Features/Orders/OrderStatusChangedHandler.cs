using MiniKobo.Contracts;
using MiniKobo.Orders.Api.Data;
using NServiceBus;

namespace MiniKobo.Orders.Api.Features.Orders;

/// <summary>Keeps the order's status (what the customer sees) in step with order processing.</summary>
public sealed class OrderStatusChangedHandler(TimeProvider timeProvider, ILogger<OrderStatusChangedHandler> logger)
    : IHandleMessages<OrderStatusChanged>
{
    public async Task Handle(OrderStatusChanged message, IMessageHandlerContext context)
    {
        var storage = context.SynchronizedStorageSession.SqlPersistenceSession();
        await using var db = OrdersDbContext.ForTransaction(storage.Connection, storage.Transaction);

        var order = await db.Orders.FindAsync([message.OrderId], context.CancellationToken);
        if (order is null)
        {
            logger.LogWarning("Status {Status} received for unknown order {OrderId}", message.Status, message.OrderId);
            return;
        }

        if (order.TryApplyStatus(message.Status, message.Reason, timeProvider.GetUtcNow()))
        {
            await db.SaveChangesAsync(context.CancellationToken);
            logger.LogInformation("Order {OrderId} is now {Status}", order.Id, order.Status);
        }
    }
}
