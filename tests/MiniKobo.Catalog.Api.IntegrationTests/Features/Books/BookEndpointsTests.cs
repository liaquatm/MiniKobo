using System.Net;
using System.Net.Http.Json;
using MiniKobo.Catalog.Api.Common;
using MiniKobo.Catalog.Api.Features.Books;

namespace MiniKobo.Catalog.Api.IntegrationTests.Features.Books;

[TestFixture]
public sealed class BookEndpointsTests
{
    private CatalogApiFactory _factory = null!;
    private HttpClient _client = null!;

    // Runs once for the whole fixture: starting a container takes seconds, so we share it across tests.
    [OneTimeSetUp]
    public async Task StartApiAsync()
    {
        _factory = new CatalogApiFactory();
        await _factory.StartContainerAsync();

        // Creating the client starts the API, which runs Program.cs, including seeding and index creation.
        _client = _factory.CreateClient();
        await _factory.WaitForIndexingAsync();
    }

    [OneTimeTearDown]
    public async Task StopApiAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Test]
    public async Task GetBooks_ReturnsAllSeededBooksSortedByTitle()
    {
        var result = await _client.GetFromJsonAsync<PagedResponse<BookResponse>>("/books");

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.TotalCount, Is.EqualTo(10));
            Assert.That(result.Items.Select(book => book.Title), Is.Ordered);
        });
    }

    [Test]
    public async Task GetBooks_WithPaging_ReturnsRequestedPage()
    {
        var all = await _client.GetFromJsonAsync<PagedResponse<BookResponse>>("/books");
        var page = await _client.GetFromJsonAsync<PagedResponse<BookResponse>>("/books?page=2&pageSize=3");

        Assert.That(page!.Items.Select(book => book.Isbn), Is.EqualTo(all!.Items.Skip(3).Take(3).Select(book => book.Isbn)));
    }

    [TestCase("dickens", "Great Expectations")]  // matches the author
    [TestCase("whale", "Moby-Dick")]             // matches the description
    [TestCase("FRANKENSTEIN", "Frankenstein")]   // case-insensitive
    public async Task GetBooks_WithSearch_ReturnsMatchingBook(string search, string expectedTitle)
    {
        var result = await _client.GetFromJsonAsync<PagedResponse<BookResponse>>($"/books?search={search}");

        Assert.That(result!.Items.Select(book => book.Title), Is.EquivalentTo(new[] { expectedTitle }));
    }

    [Test]
    public async Task GetBooks_WithInvalidPageSize_ReturnsValidationProblem()
    {
        var response = await _client.GetAsync($"/books?pageSize={GetBooks.MaxPageSize + 1}");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        });
    }

    [Test]
    public async Task GetBookByIsbn_WithKnownIsbn_ReturnsBook()
    {
        var book = await _client.GetFromJsonAsync<BookResponse>("/books/9780000000009");

        Assert.That(book!.Title, Is.EqualTo("The Count of Monte Cristo"));
    }

    [Test]
    public async Task GetBookByIsbn_WithUnknownIsbn_ReturnsNotFoundProblem()
    {
        var response = await _client.GetAsync("/books/0000000000000");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        });
    }
}
