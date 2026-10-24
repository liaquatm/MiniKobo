using Microsoft.AspNetCore.Http.HttpResults;
using MiniKobo.Catalog.Api.Common;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace MiniKobo.Catalog.Api.Features.Books;

public static class GetBooks
{
    private const int MaxPageSize = 50;

    public static RouteHandlerBuilder MapGetBooks(this IEndpointRouteBuilder app) =>
        app.MapGet("/", HandleAsync);

    public static async Task<Results<Ok<PagedResponse<BookResponse>>, ValidationProblem>> HandleAsync(
        IAsyncDocumentSession session,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1 || pageSize is < 1 or > MaxPageSize)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["paging"] = [$"page must be at least 1 and pageSize must be between 1 and {MaxPageSize}."]
            });
        }

        var books = await session.Query<Book>()
            .Statistics(out var stats)
            .OrderBy(book => book.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = books.Select(BookResponse.FromBook).ToList();

        return TypedResults.Ok(new PagedResponse<BookResponse>(items, page, pageSize, stats.TotalResults));
    }
}