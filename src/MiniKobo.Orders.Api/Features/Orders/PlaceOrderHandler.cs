using Microsoft.EntityFrameworkCore;
using MiniKobo.Contracts;
using MiniKobo.Orders.Api.Data;
using NServiceBus;

namespace MiniKobo.Orders.Api.Features.Orders;

public sealed class PlaceOrderHandler(TimeProvider timeProvider, ILogger<PlaceOrderHandler> logger)
    : IHandleMessages<PlaceOrder>
{
    public async Task Handle(PlaceOrder message, IMessageHandlerContext context)
    {
        // Join NServiceBus's connection and transaction: the order, the cart deletion and the
        // outgoing OrderPlaced event (in the outbox) all commit together, or not at all.
        var storage = context.SynchronizedStorageSession.SqlPersistenceSession();
        await using var db = OrdersDbContext.ForTransaction(storage.Connection, storage.Transaction);

        if (await db.Orders.AnyAsync(order => order.Id == message.OrderId, context.CancellationToken))
        {
            return; // Already handled (duplicate message).
        }

        var cart = await db.Carts
            .Include(c => c.Items)
            .SingleOrDefaultAsync(c => c.Id == message.CartId, context.CancellationToken);

        if (cart is null || cart.Items.Count == 0)
        {
            logger.LogWarning("Order {OrderId} not placed: cart {CartId} is missing or empty", message.OrderId, message.CartId);
            return;
        }

        var order = Order.FromCart(message.OrderId, cart, message.CustomerName, message.CustomerEmail, timeProvider.GetUtcNow());
        db.Orders.Add(order);
        db.Carts.Remove(cart);
        await db.SaveChangesAsync(context.CancellationToken);

        await context.Publish(new OrderPlaced(
            order.Id,
            order.CustomerName,
            order.CustomerEmail,
            order.Total,
            order.Items.Select(item => new OrderLine(item.Isbn, item.Title, item.UnitPrice, item.Quantity)).ToList()));

        logger.LogInformation("Order {OrderId} placed for {Total}", order.Id, order.Total);
    }
}
