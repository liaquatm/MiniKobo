using MiniKobo.Contracts;
using MiniKobo.Orders.Api.Features.Carts;

namespace MiniKobo.Orders.Api.Features.Orders;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    public Order(Guid id, string customerName, string customerEmail, DateTimeOffset createdAt)
    {
        Id = id;
        CustomerName = customerName;
        CustomerEmail = customerEmail;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Status = OrderStatus.Placed;
    }

    public Guid Id { get; private set; }
    public string CustomerName { get; private set; }
    public string CustomerEmail { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items;
    public decimal Total => _items.Sum(item => item.LineTotal);

    public static Order FromCart(Guid orderId, Cart cart, string customerName, string customerEmail, DateTimeOffset now)
    {
        var order = new Order(orderId, customerName, customerEmail, now);
        order._items.AddRange(cart.Items.Select(item =>
            new OrderItem(orderId, item.Isbn, item.Title, item.UnitPrice, item.Quantity)));
        return order;
    }

    /// <summary>
    /// Moves the order forward. Events can arrive late, twice, or out of order, so a status
    /// is only applied if it moves the order forward; Confirmed and Failed are final.
    /// </summary>
    public bool TryApplyStatus(OrderStatus newStatus, string? reason, DateTimeOffset now)
    {
        if (Status is OrderStatus.Confirmed or OrderStatus.Failed)
        {
            return false;
        }

        if (newStatus != OrderStatus.Failed && newStatus <= Status)
        {
            return false;
        }

        Status = newStatus;
        FailureReason = reason;
        UpdatedAt = now;
        return true;
    }
}

public sealed class OrderItem(Guid orderId, string isbn, string title, decimal unitPrice, int quantity)
{
    public Guid OrderId { get; private set; } = orderId;
    public string Isbn { get; private set; } = isbn;
    public string Title { get; private set; } = title;
    public decimal UnitPrice { get; private set; } = unitPrice;
    public int Quantity { get; private set; } = quantity;
    public decimal LineTotal => UnitPrice * Quantity;
}
