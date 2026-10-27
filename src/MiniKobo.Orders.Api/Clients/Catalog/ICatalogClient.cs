namespace MiniKobo.Orders.Api.Clients.Catalog;

public interface ICatalogClient
{
    /// <summary>
    /// Returns the book, or null if the Catalog has no book with this ISBN.
    /// </summary>
    /// <exception cref="CatalogUnavailableException">The Catalog API could not be reached or failed.</exception>
    Task<CatalogBook?> GetBookAsync(string isbn, CancellationToken cancellationToken);
}

public sealed class CatalogUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
