using System.Net;
using System.Net.Http.Json;
using MiniKobo.Orders.Api.Features.Carts;
using MiniKobo.Orders.Api.Features.Orders;

namespace MiniKobo.Orders.Api.IntegrationTests.Features;

[TestFixture]
public sealed class CartAndOrderEndpointsTests
{
    private const string Pride = "9780000000001";
    private const string MonteCristo = "9780000000009"; // only 1 in stock in the fake catalog

    private OrdersApiFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task StartApiAsync()
    {
        _factory = new OrdersApiFactory();
        await _factory.StartContainerAsync();   // the connection string only exists once it's running
        _client = _factory.CreateClient();      // starts the API
        await _factory.MigrateAsync();          // creates the tables
    }

    [OneTimeTearDown]
    public async Task StopApiAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [SetUp]
    public void ResetFakes() => _factory.Catalog.IsUnavailable = false;

    [Test]
    public async Task CreateCart_Returns201WithLocation()
    {
        var response = await _client.PostAsync("/carts", null);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers.Location?.AbsolutePath, Does.StartWith("/carts/"));
        });
    }

    [Test]
    public async Task SetItem_StoresTheCatalogTitleAndPrice()
    {
        var cartId = await CreateCartAsync();

        var response = await SetItemAsync(cartId, Pride, 2);
        var cart = await _client.GetFromJsonAsync<CartResponse>($"/carts/{cartId}");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(cart!.Items.Single().Title, Is.EqualTo("Pride and Prejudice"));
            Assert.That(cart.Total, Is.EqualTo(17.98m));
        });
    }

    [TestCase(MonteCristo, 2, HttpStatusCode.Conflict)]           // more than in stock
    [TestCase("0000000000000", 1, HttpStatusCode.NotFound)]       // not in the catalog
    [TestCase(Pride, 11, HttpStatusCode.BadRequest)]              // over the 1-10 limit
    public async Task SetItem_RejectsInvalidRequests(string isbn, int quantity, HttpStatusCode expected)
    {
        var cartId = await CreateCartAsync();

        var response = await SetItemAsync(cartId, isbn, quantity);

        Assert.That(response.StatusCode, Is.EqualTo(expected));
    }

    [Test]
    public async Task SetItem_WhenCatalogIsDown_Returns503()
    {
        var cartId = await CreateCartAsync();
        _factory.Catalog.IsUnavailable = true;

        var response = await SetItemAsync(cartId, Pride, 1);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task PlaceOrder_SendsPlaceOrderCommandAndReturns202()
    {
        var cartId = await CreateCartAsync();
        await SetItemAsync(cartId, Pride, 1);

        var response = await _client.PostAsJsonAsync("/orders",
            new { cartId, customerName = "Ann", customerEmail = "ann@example.com" });
        var body = await response.Content.ReadFromJsonAsync<PlaceOrderResponse>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(response.Headers.Location?.AbsolutePath, Is.EqualTo($"/orders/{body!.OrderId}"));
            Assert.That(
                _factory.MessageSession.SentMessages.Select(sent => sent.Message).OfType<PlaceOrder>()
                    .Any(command => command.OrderId == body.OrderId && command.CartId == cartId),
                Is.True);
        });
    }

    [Test]
    public async Task PlaceOrder_WithEmptyCart_Returns400()
    {
        var cartId = await CreateCartAsync();

        var response = await _client.PostAsJsonAsync("/orders",
            new { cartId, customerName = "Ann", customerEmail = "ann@example.com" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private async Task<Guid> CreateCartAsync()
    {
        var response = await _client.PostAsync("/carts", null);
        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();
        return cart!.Id;
    }

    private Task<HttpResponseMessage> SetItemAsync(Guid cartId, string isbn, int quantity) =>
        _client.PutAsJsonAsync($"/carts/{cartId}/items/{isbn}", new { quantity });
}
