namespace MiniKobo.Catalog.Api.Features.Books;

public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        var books = app.MapGroup("/books").WithTags("Books");

        books.MapGetBooks();
        books.MapGetBookByIsbn();

        return app;
    }
}