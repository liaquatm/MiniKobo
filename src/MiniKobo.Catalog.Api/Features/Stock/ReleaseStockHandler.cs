using MiniKobo.Catalog.Api.Features.Books;
using MiniKobo.Contracts;
using NServiceBus;
using Raven.Client.Documents;

namespace MiniKobo.Catalog.Api.Features.Stock;

public sealed class ReleaseStockHandler(IDocumentStore store, ILogger<ReleaseStockHandler> logger)
    : IHandleMessages<ReleaseStock>
{
    public async Task Handle(ReleaseStock message, IMessageHandlerContext context)
    {
        using var session = store.OpenAsyncSession();
        session.Advanced.UseOptimisticConcurrency = true;

        var reservation = await session.LoadAsync<StockReservation>(
            StockReservation.IdFor(message.OrderId), context.CancellationToken);

        // Only give stock back once, and only if it was actually taken (idempotent).
        if (reservation is not { Status: ReservationStatus.Reserved })
        {
            return;
        }

        var books = await session.LoadAsync<Book>(
            reservation.Lines.Select(line => $"books/{line.Isbn}"), context.CancellationToken);

        foreach (var line in reservation.Lines)
        {
            if (books[$"books/{line.Isbn}"] is { } book)
            {
                book.StockQuantity += line.Quantity;
            }
        }

        reservation.Status = ReservationStatus.Released;
        await session.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Stock released for order {OrderId}", message.OrderId);
    }
}
