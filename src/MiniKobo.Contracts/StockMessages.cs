namespace MiniKobo.Contracts;

public sealed record StockLine(string Isbn, int Quantity);

/// <summary>Command to Catalog: reserve stock for every line of an order, all or nothing.</summary>
public sealed record ReserveStock(Guid OrderId, IReadOnlyList<StockLine> Lines) : ICommand;

/// <summary>Command to Catalog: give back stock reserved for an order (compensation).</summary>
public sealed record ReleaseStock(Guid OrderId) : ICommand;

// Replies to ReserveStock: plain messages, neither commands nor events.
public sealed record StockReserved(Guid OrderId) : IMessage;

public sealed record StockReservationFailed(Guid OrderId, string Reason) : IMessage;
