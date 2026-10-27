namespace MiniKobo.Orders.Api.Clients.Catalog;

/// <summary>
/// The Orders API's own view of a Catalog book: only the fields it needs.
/// Extra fields in Catalog's JSON are ignored, so Catalog can add fields without breaking Orders.
/// </summary>
public sealed record CatalogBook(string Isbn, string Title, decimal Price, int StockQuantity);
