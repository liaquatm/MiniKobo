namespace MiniKobo.Orders.Api.Features.Carts;

public sealed class CartItem
{
    public CartItem(Guid cartId, string isbn, string title, decimal unitPrice, int quantity)
    {
        CartId = cartId;
        Isbn = isbn;
        Title = title;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid CartId { get; private set; }
    public string Isbn { get; private set; }

    // Title and price are copied from the Catalog when the item is added (a snapshot).
    public string Title { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
}
