using MiniKobo.Orders.Api.Features.Carts;

namespace MiniKobo.Orders.Api.UnitTests.Features.Carts;

[TestFixture]
public sealed class CartTests
{
    private static readonly DateTimeOffset Created = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Created.AddMinutes(5);

    [Test]
    public void SetItemQuantity_NewBook_AddsLineAndUpdatesTimestamp()
    {
        var cart = new Cart(Guid.NewGuid(), Created);

        cart.SetItemQuantity("9780000000001", "Pride and Prejudice", 8.99m, 2, Later);

        Assert.Multiple(() =>
        {
            Assert.That(cart.Items, Has.Count.EqualTo(1));
            Assert.That(cart.Items[0].Quantity, Is.EqualTo(2));
            Assert.That(cart.Total, Is.EqualTo(17.98m));
            Assert.That(cart.UpdatedAt, Is.EqualTo(Later));
        });
    }

    [Test]
    public void SetItemQuantity_BookAlreadyInCart_UpdatesTheSameLine()
    {
        var cart = new Cart(Guid.NewGuid(), Created);
        cart.SetItemQuantity("9780000000001", "Pride and Prejudice", 8.99m, 2, Later);

        cart.SetItemQuantity("9780000000001", "Pride and Prejudice", 8.99m, 5, Later);

        Assert.Multiple(() =>
        {
            Assert.That(cart.Items, Has.Count.EqualTo(1));
            Assert.That(cart.Items[0].Quantity, Is.EqualTo(5));
        });
    }

    [TestCase(0)]
    [TestCase(Cart.MaxQuantity + 1)]
    public void SetItemQuantity_OutOfRange_Throws(int quantity)
    {
        var cart = new Cart(Guid.NewGuid(), Created);

        Assert.That(
            () => cart.SetItemQuantity("9780000000001", "Pride and Prejudice", 8.99m, quantity, Later),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void RemoveItem_ReturnsWhetherTheBookWasInTheCart()
    {
        var cart = new Cart(Guid.NewGuid(), Created);
        cart.SetItemQuantity("9780000000001", "Pride and Prejudice", 8.99m, 1, Later);

        Assert.Multiple(() =>
        {
            Assert.That(cart.RemoveItem("9780000000001", Later), Is.True);
            Assert.That(cart.RemoveItem("9780000000001", Later), Is.False);
            Assert.That(cart.Items, Is.Empty);
        });
    }
}
