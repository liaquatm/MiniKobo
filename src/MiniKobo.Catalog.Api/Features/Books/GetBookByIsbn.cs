using Microsoft.AspNetCore.Http.HttpResults;
using Raven.Client.Documents.Session;

namespace MiniKobo.Catalog.Api.Features.Books;

public static class GetBookByIsbn
{
    public static RouteHandlerBuilder MapGetBookByIsbn(this IEndpointRouteBuilder app) =>
        app.MapGet("/{isbn}", HandleAsync);

    public static async Task<Results<Ok<BookResponse>, NotFound>> HandleAsync(
        string isbn,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken)
    {
        var book = await session.LoadAsync<Book>($"books/{isbn}", cancellationToken);

        if (book is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(BookResponse.FromBook(book));
    }
}