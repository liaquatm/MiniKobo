using MiniKobo.Contracts;
using MiniKobo.Orders.Api.Features.Orders;

namespace MiniKobo.Orders.Api.UnitTests.Features.Orders;

[TestFixture]
public sealed class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static Order NewOrder() => new(Guid.NewGuid(), "Ann", "ann@example.com", Now);

    [Test]
    public void TryApplyStatus_MovesForward()
    {
        var order = NewOrder();

        Assert.Multiple(() =>
        {
            Assert.That(order.TryApplyStatus(OrderStatus.StockReserved, null, Now), Is.True);
            Assert.That(order.Status, Is.EqualTo(OrderStatus.StockReserved));
        });
    }

    [Test]
    public void TryApplyStatus_IgnoresALateOrDuplicateEvent()
    {
        // Events can arrive out of order: Paid may be processed before StockReserved.
        var order = NewOrder();
        order.TryApplyStatus(OrderStatus.Paid, null, Now);

        Assert.Multiple(() =>
        {
            Assert.That(order.TryApplyStatus(OrderStatus.StockReserved, null, Now), Is.False);
            Assert.That(order.Status, Is.EqualTo(OrderStatus.Paid));
        });
    }

    [Test]
    public void TryApplyStatus_FailedIsFinalAndKeepsTheReason()
    {
        var order = NewOrder();
        order.TryApplyStatus(OrderStatus.Failed, "Payment was declined.", Now);

        Assert.Multiple(() =>
        {
            Assert.That(order.TryApplyStatus(OrderStatus.Confirmed, null, Now), Is.False);
            Assert.That(order.Status, Is.EqualTo(OrderStatus.Failed));
            Assert.That(order.FailureReason, Is.EqualTo("Payment was declined."));
        });
    }
}
