namespace MiniKobo.Catalog.Api.Features.Books;

public sealed record BookResponse(
    string Isbn,
    string Title,
    string Author,
    string Description,
    decimal Price,
    int StockQuantity)
{
    public static BookResponse FromBook(Book book) =>
        new(book.Isbn, book.Title, book.Author, book.Description, book.Price, book.StockQuantity);
}