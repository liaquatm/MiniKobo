using Microsoft.Extensions.Logging.Abstractions;
using MiniKobo.Contracts;
using NServiceBus.Testing;

namespace MiniKobo.Orders.Worker.UnitTests;

/// <summary>
/// Saga tests call each handler directly with a fake context that records what the saga
/// sent and published, so no broker or database is needed.
/// </summary>
[TestFixture]
public sealed class OrderProcessingSagaTests
{
    private static readonly Guid OrderId = Guid.NewGuid();

    private static OrderProcessingSaga NewSaga() =>
        new(NullLogger<OrderProcessingSaga>.Instance)
        {
            Data = new OrderProcessingSagaData
            {
                OrderId = OrderId,
                CustomerName = "Ann",
                CustomerEmail = "ann@example.com",
                Total = 17.98m,
                Lines = [new OrderLine("9780000000001", "Pride and Prejudice", 8.99m, 2)]
            }
        };

    [Test]
    public async Task OrderPlaced_AsksCatalogToReserveStock()
    {
        var saga = NewSaga();
        var context = new TestableMessageHandlerContext();

        await saga.Handle(
            new OrderPlaced(OrderId, "Ann", "ann@example.com", 17.98m,
                [new OrderLine("9780000000001", "Pride and Prejudice", 8.99m, 2)]),
            context);

        var reserve = context.SentMessages.Single().Message as ReserveStock;
        Assert.That(reserve, Is.Not.Null);
        Assert.That(reserve!.Lines.Single(), Is.EqualTo(new StockLine("9780000000001", 2)));
    }

    [Test]
    public async Task StockReserved_PublishesStatusAndRequestsPayment()
    {
        var saga = NewSaga();
        var context = new TestableMessageHandlerContext();

        await saga.Handle(new StockReserved(OrderId), context);

        Assert.Multiple(() =>
        {
            Assert.That(context.PublishedMessages.Single().Message,
                Is.EqualTo(new OrderStatusChanged(OrderId, OrderStatus.StockReserved)));
            Assert.That(context.SentMessages.Single().Message, Is.TypeOf<ProcessPayment>());
            Assert.That(saga.Completed, Is.False);
        });
    }

    [Test]
    public async Task PaymentFailed_ReleasesStockAndFailsTheOrder()
    {
        var saga = NewSaga();
        var context = new TestableMessageHandlerContext();

        await saga.Handle(new PaymentFailed(OrderId, "Declined"), context);

        Assert.Multiple(() =>
        {
            // Compensation: the stock reserved earlier must be given back.
            Assert.That(context.SentMessages.Single().Message, Is.EqualTo(new ReleaseStock(OrderId)));
            Assert.That(context.PublishedMessages.Single().Message,
                Is.EqualTo(new OrderStatusChanged(OrderId, OrderStatus.Failed, "Declined")));
            Assert.That(saga.Completed, Is.True);
        });
    }

    [Test]
    public async Task StockReservationFailed_FailsTheOrderWithoutCompensation()
    {
        var saga = NewSaga();
        var context = new TestableMessageHandlerContext();

        await saga.Handle(new StockReservationFailed(OrderId, "Not enough stock."), context);

        Assert.Multiple(() =>
        {
            Assert.That(context.SentMessages, Is.Empty);
            Assert.That(saga.Completed, Is.True);
        });
    }

    [Test]
    public async Task ConfirmationEmailSent_ConfirmsAndCompletes()
    {
        var saga = NewSaga();
        var context = new TestableMessageHandlerContext();

        await saga.Handle(new ConfirmationEmailSent(OrderId), context);

        Assert.Multiple(() =>
        {
            Assert.That(context.PublishedMessages.Single().Message,
                Is.EqualTo(new OrderStatusChanged(OrderId, OrderStatus.Confirmed)));
            Assert.That(saga.Completed, Is.True);
        });
    }
}
