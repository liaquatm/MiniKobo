using MiniKobo.Orders.Api.Clients.Catalog;

namespace MiniKobo.Orders.Api.IntegrationTests;

/// <summary>Stands in for the Catalog API, so Orders can be tested on its own (decision D9).</summary>
public sealed class FakeCatalogClient : ICatalogClient
{
    public Dictionary<string, CatalogBook> Books { get; } = new()
    {
        ["9780000000001"] = new("9780000000001", "Pride and Prejudice", 8.99m, 25),
        ["9780000000009"] = new("9780000000009", "The Count of Monte Cristo", 12.99m, 1)
    };

    public bool IsUnavailable { get; set; }

    public Task<CatalogBook?> GetBookAsync(string isbn, CancellationToken cancellationToken) =>
        IsUnavailable
            ? Task.FromException<CatalogBook?>(new CatalogUnavailableException("Catalog is down (fake)."))
            : Task.FromResult(Books.GetValueOrDefault(isbn));
}
