using NServiceBus;

namespace MiniKobo.Orders.Api.Features.Orders;

/// <summary>
/// Internal command the API sends to its own queue. Handling it in a message handler lets the
/// outbox save the order and publish OrderPlaced atomically.
/// </summary>
public sealed record PlaceOrder(Guid OrderId, Guid CartId, string CustomerName, string CustomerEmail) : ICommand;
