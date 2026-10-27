using MiniKobo.Catalog.Api.Features.Books;
using MiniKobo.Contracts;
using NServiceBus;
using Raven.Client.Documents;

namespace MiniKobo.Catalog.Api.Features.Stock;

public sealed class ReserveStockHandler(IDocumentStore store, ILogger<ReserveStockHandler> logger)
    : IHandleMessages<ReserveStock>
{
    public async Task Handle(ReserveStock message, IMessageHandlerContext context)
    {
        using var session = store.OpenAsyncSession();
        // If another order changes the same book between our load and save, SaveChanges throws and
        // NServiceBus retries the message, which re-reads the latest stock. No overselling.
        session.Advanced.UseOptimisticConcurrency = true;

        var existing = await session.LoadAsync<StockReservation>(
            StockReservation.IdFor(message.OrderId), context.CancellationToken);
        if (existing is not null)
        {
            // Duplicate or retried message: answer the same way again, without touching stock.
            await ReplyAsync(existing, context);
            return;
        }

        var books = await session.LoadAsync<Book>(
            message.Lines.Select(line => BookId(line.Isbn)), context.CancellationToken);

        var shortages = message.Lines
            .Where(line => books[BookId(line.Isbn)] is not { } book || book.StockQuantity < line.Quantity)
            .Select(line => books[BookId(line.Isbn)]?.Title ?? line.Isbn)
            .ToList();

        var reservation = new StockReservation
        {
            Id = StockReservation.IdFor(message.OrderId),
            OrderId = message.OrderId,
            Lines = message.Lines.ToList(),
            Status = shortages.Count == 0 ? ReservationStatus.Reserved : ReservationStatus.Rejected,
            Reason = shortages.Count == 0 ? null : $"Not enough stock for: {string.Join(", ", shortages)}."
        };

        if (reservation.Status == ReservationStatus.Reserved)
        {
            foreach (var line in message.Lines)
            {
                books[BookId(line.Isbn)]!.StockQuantity -= line.Quantity;
            }
        }

        await session.StoreAsync(reservation, context.CancellationToken);
        await session.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Stock {Status} for order {OrderId}", reservation.Status, message.OrderId);
        await ReplyAsync(reservation, context);
    }

    private static Task ReplyAsync(StockReservation reservation, IMessageHandlerContext context) =>
        reservation.Status == ReservationStatus.Rejected
            ? context.Reply(new StockReservationFailed(reservation.OrderId, reservation.Reason ?? "Not enough stock."))
            : context.Reply(new StockReserved(reservation.OrderId));

    private static string BookId(string isbn) => $"books/{isbn}";
}
