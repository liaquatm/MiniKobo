namespace MiniKobo.Catalog.Api.Features.Books;

public sealed class Book
{
    public string? Id { get; set; }
    public required string Title { get; init; }
    public required string Author { get; init; }
    public required string Isbn { get; init; }
    public required string Description { get; init; }
    public required decimal Price { get; init; }
    public int StockQuantity { get; set; }
}