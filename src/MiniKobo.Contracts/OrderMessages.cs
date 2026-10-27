namespace MiniKobo.Contracts;

// Messages are the contract between services: the only code the services share.

public sealed record OrderLine(string Isbn, string Title, decimal UnitPrice, int Quantity);

/// <summary>Published by Orders once an order has been saved. Starts order processing.</summary>
public sealed record OrderPlaced(
    Guid OrderId,
    string CustomerName,
    string CustomerEmail,
    decimal Total,
    IReadOnlyList<OrderLine> Lines) : IEvent;

public enum OrderStatus
{
    Placed,
    StockReserved,
    Paid,
    Confirmed,
    Failed
}

/// <summary>Published by order processing whenever an order moves to a new status.</summary>
public sealed record OrderStatusChanged(Guid OrderId, OrderStatus Status, string? Reason = null) : IEvent;
