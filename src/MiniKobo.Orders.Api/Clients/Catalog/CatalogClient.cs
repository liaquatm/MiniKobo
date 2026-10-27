using System.Net;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace MiniKobo.Orders.Api.Clients.Catalog;

public sealed class CatalogClient(HttpClient httpClient) : ICatalogClient
{
    public async Task<CatalogBook?> GetBookAsync(string isbn, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync($"books/{Uri.EscapeDataString(isbn)}", cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TimeoutRejectedException or BrokenCircuitException)
        {
            // Reached only after the resilience handler has given up (retries exhausted, timed out, or circuit open).
            throw new CatalogUnavailableException("The Catalog API could not be reached.", exception);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new CatalogUnavailableException($"The Catalog API returned {(int)response.StatusCode}.");
            }

            return await response.Content.ReadFromJsonAsync<CatalogBook>(cancellationToken);
        }
    }
}
