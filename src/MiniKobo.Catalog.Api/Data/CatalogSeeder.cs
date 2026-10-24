using MiniKobo.Catalog.Api.Features.Books;
using Raven.Client.Documents;

namespace MiniKobo.Catalog.Api.Data;

public static class CatalogSeeder
{
    public static async Task SeedAsync(IDocumentStore store, CancellationToken cancellationToken = default)
    {
        using var session = store.OpenAsyncSession();

        var seedBooks = CreateSeedBooks();
        var existing = await session.LoadAsync<Book>(seedBooks.Select(book => book.Id!), cancellationToken);

        foreach (var book in seedBooks.Where(book => existing[book.Id!] is null))
        {
            await session.StoreAsync(book, cancellationToken);
        }

        await session.SaveChangesAsync(cancellationToken);
    }

    private static Book[] CreateSeedBooks() =>
    [
        Create("9780000000001", "Pride and Prejudice", "Jane Austen", 8.99m, 25,
            "Elizabeth Bennet trades sharp words with the proud Mr Darcy in Regency England."),
        Create("9780000000002", "Moby-Dick", "Herman Melville", 10.99m, 12,
            "Captain Ahab's obsessive hunt for the white whale that took his leg."),
        Create("9780000000003", "Frankenstein", "Mary Shelley", 7.99m, 18,
            "A young scientist creates life and is haunted by the consequences."),
        Create("9780000000004", "Dracula", "Bram Stoker", 8.49m, 9,
            "An ancient count travels to England, told through letters and diaries."),
        Create("9780000000005", "Jane Eyre", "Charlotte Brontë", 8.99m, 14,
            "An orphaned governess finds independence, love and a dark secret at Thornfield Hall."),
        Create("9780000000006", "Wuthering Heights", "Emily Brontë", 7.49m, 7,
            "A turbulent love between Catherine and Heathcliff on the Yorkshire moors."),
        Create("9780000000007", "Great Expectations", "Charles Dickens", 9.49m, 20,
            "The orphan Pip rises in society thanks to a mysterious benefactor."),
        Create("9780000000008", "The Picture of Dorian Gray", "Oscar Wilde", 6.99m, 11,
            "A portrait ages and decays while its subject stays young."),
        Create("9780000000009", "The Count of Monte Cristo", "Alexandre Dumas", 12.99m, 1,
            "A wrongly imprisoned sailor escapes and plots an elaborate revenge."),
        Create("9780000000010", "Crime and Punishment", "Fyodor Dostoevsky", 11.49m, 0,
            "A poor student commits a murder and wrestles with guilt in St Petersburg.")
    ];

    private static Book Create(string isbn, string title, string author, decimal price, int stock, string description) =>
        new()
        {
            Id = $"books/{isbn}",
            Isbn = isbn,
            Title = title,
            Author = author,
            Price = price,
            StockQuantity = stock,
            Description = description
        };
}