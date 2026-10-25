using MiniKobo.Catalog.Api.Data;

namespace MiniKobo.Catalog.Api.UnitTests.Data;

[TestFixture]
public sealed class CatalogSeederTests
{
    [Test]
    public void SeedBooks_HaveUniqueIsbns()
    {
        var isbns = CatalogSeeder.CreateSeedBooks().Select(book => book.Isbn);

        Assert.That(isbns, Is.Unique);
    }

    [Test]
    public void SeedBooks_HaveIdsDerivedFromIsbn()
    {
        // The seeder's idempotency check relies on predictable IDs.
        foreach (var book in CatalogSeeder.CreateSeedBooks())
        {
            Assert.That(book.Id, Is.EqualTo($"books/{book.Isbn}"));
        }
    }

    [Test]
    public void SeedBooks_IncludeStockEdgeCasesForOrderTests()
    {
        var stock = CatalogSeeder.CreateSeedBooks().Select(book => book.StockQuantity).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(stock, Has.Member(0), "an out-of-stock book");
            Assert.That(stock, Has.Member(1), "a book with only one copy left");
        });
    }
}
