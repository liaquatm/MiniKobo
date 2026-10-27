namespace MiniKobo.Orders.Api.Features.Carts;

public sealed class Cart
{
    public const int MinQuantity = 1;
    public const int MaxQuantity = 10;

    // EF Core reads and writes this field directly; callers only see the read-only Items view.
    private readonly List<CartItem> _items = [];

    public Cart(Guid id, DateTimeOffset createdAt)
    {
        Id = id;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<CartItem> Items => _items;
    public decimal Total => _items.Sum(item => item.LineTotal);

    /// <summary>
    /// Adds the book, or updates its line if it is already in the cart.
    /// </summary>
    public void SetItemQuantity(string isbn, string title, decimal unitPrice, int quantity, DateTimeOffset now)
    {
        if (quantity is < MinQuantity or > MaxQuantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity), quantity, $"Quantity must be between {MinQuantity} and {MaxQuantity}.");
        }

        var existing = _items.Find(item => item.Isbn == isbn);
        if (existing is null)
        {
            _items.Add(new CartItem(Id, isbn, title, unitPrice, quantity));
        }
        else
        {
            existing.Update(title, unitPrice, quantity);
        }

        UpdatedAt = now;
    }

    /// <summary>
    /// Removes the book from the cart. Returns false if it was not in the cart.
    /// </summary>
    public bool RemoveItem(string isbn, DateTimeOffset now)
    {
        var removed = _items.RemoveAll(item => item.Isbn == isbn) > 0;
        if (removed)
        {
            UpdatedAt = now;
        }

        return removed;
    }
}
