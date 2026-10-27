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
    public decimal LineTotal => UnitPrice * Quantity;

    // Only Cart should call this; it validates the quantity first.
    internal void Update(string title, decimal unitPrice, int quantity)
    {
        Title = title;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}
