namespace MiniKobo.Orders.Api.Features.Carts;

public sealed class Cart
{
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
}
