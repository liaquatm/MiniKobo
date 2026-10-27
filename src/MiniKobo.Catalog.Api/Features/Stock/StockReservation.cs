using MiniKobo.Contracts;

namespace MiniKobo.Catalog.Api.Features.Stock;

public enum ReservationStatus
{
    Reserved,
    Rejected,
    Released
}

/// <summary>
/// Records what happened to an order's ReserveStock request, so a retried or duplicate
/// message gets the same answer instead of taking stock twice (idempotency).
/// </summary>
public sealed class StockReservation
{
    public string? Id { get; set; }
    public required Guid OrderId { get; init; }
    public required List<StockLine> Lines { get; init; }
    public ReservationStatus Status { get; set; }
    public string? Reason { get; set; }

    public static string IdFor(Guid orderId) => $"reservations/{orderId}";
}
