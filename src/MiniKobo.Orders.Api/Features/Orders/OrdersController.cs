using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniKobo.Orders.Api.Data;
using NServiceBus;

namespace MiniKobo.Orders.Api.Features.Orders;

public sealed record PlaceOrderRequest(
    [Required] Guid? CartId,
    [Required, StringLength(200, MinimumLength = 1)] string CustomerName,
    [Required, EmailAddress, StringLength(320)] string CustomerEmail);

public sealed record PlaceOrderResponse(Guid OrderId);

public sealed record OrderItemResponse(string Isbn, string Title, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record OrderResponse(
    Guid Id,
    string Status,
    string? FailureReason,
    string CustomerName,
    string CustomerEmail,
    IReadOnlyList<OrderItemResponse> Items,
    decimal Total,
    DateTimeOffset CreatedAt)
{
    public static OrderResponse FromOrder(Order order) => new(
        order.Id,
        order.Status.ToString(),
        order.FailureReason,
        order.CustomerName,
        order.CustomerEmail,
        order.Items.Select(i => new OrderItemResponse(i.Isbn, i.Title, i.UnitPrice, i.Quantity, i.LineTotal)).ToList(),
        order.Total,
        order.CreatedAt);
}

[ApiController]
[Route("orders")]
public sealed class OrdersController(OrdersDbContext db, IMessageSession messageSession) : ControllerBase
{
    /// <summary>
    /// Accepts the order and returns immediately (202). The order is created moments later by
    /// PlaceOrderHandler; clients poll GET /orders/{id} to follow it.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PlaceOrderResponse>> PlaceOrder(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var cart = await db.Carts
            .AsNoTracking()
            .Include(c => c.Items)
            .SingleOrDefaultAsync(c => c.Id == request.CartId, cancellationToken);

        if (cart is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Cart not found.");
        }

        if (cart.Items.Count == 0)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Your cart is empty.");
        }

        var orderId = Guid.CreateVersion7();
        await messageSession.Send(
            new PlaceOrder(orderId, cart.Id, request.CustomerName.Trim(), request.CustomerEmail.Trim()),
            cancellationToken);

        return AcceptedAtAction(nameof(GetOrder), new { orderId }, new PlaceOrderResponse(orderId));
    }

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Order not found.");
        }

        return OrderResponse.FromOrder(order);
    }
}
