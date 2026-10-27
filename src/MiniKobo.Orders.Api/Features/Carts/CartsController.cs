using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniKobo.Orders.Api.Clients.Catalog;
using MiniKobo.Orders.Api.Data;

namespace MiniKobo.Orders.Api.Features.Carts;

[ApiController]
[Route("carts")]
public sealed class CartsController(
    OrdersDbContext db,
    ICatalogClient catalog,
    TimeProvider timeProvider,
    ILogger<CartsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CartResponse>> CreateCart(CancellationToken cancellationToken)
    {
        var cart = new Cart(Guid.CreateVersion7(), timeProvider.GetUtcNow());

        db.Carts.Add(cart);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCart), new { cartId = cart.Id }, CartResponse.FromCart(cart));
    }

    [HttpGet("{cartId:guid}")]
    public async Task<ActionResult<CartResponse>> GetCart(Guid cartId, CancellationToken cancellationToken)
    {
        var cart = await FindCartAsync(cartId, cancellationToken);
        if (cart is null)
        {
            return CartNotFound();
        }

        return CartResponse.FromCart(cart);
    }

    [HttpPut("{cartId:guid}/items/{isbn}")]
    public async Task<ActionResult<CartResponse>> SetItem(
        Guid cartId,
        string isbn,
        SetCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var cart = await FindCartAsync(cartId, cancellationToken);
        if (cart is null)
        {
            return CartNotFound();
        }

        CatalogBook? book;
        try
        {
            book = await catalog.GetBookAsync(isbn, cancellationToken);
        }
        catch (CatalogUnavailableException exception)
        {
            logger.LogWarning(exception, "Catalog unavailable while setting {Isbn} in cart {CartId}", isbn, cartId);
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "The catalog is temporarily unavailable. Please try again shortly.");
        }

        if (book is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Book not found.",
                detail: $"The catalog has no book with ISBN '{isbn}'.");
        }

        // A friendly early check only: stock is actually reserved at checkout (M4).
        if (request.Quantity > book.StockQuantity)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Not enough stock.",
                detail: $"Requested {request.Quantity}, but only {book.StockQuantity} in stock.");
        }

        cart.SetItemQuantity(book.Isbn, book.Title, book.Price, request.Quantity, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        return CartResponse.FromCart(cart);
    }

    [HttpDelete("{cartId:guid}/items/{isbn}")]
    public async Task<IActionResult> RemoveItem(Guid cartId, string isbn, CancellationToken cancellationToken)
    {
        var cart = await FindCartAsync(cartId, cancellationToken);
        if (cart is null)
        {
            return CartNotFound();
        }

        if (!cart.RemoveItem(isbn, timeProvider.GetUtcNow()))
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Item not in cart.");
        }

        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private Task<Cart?> FindCartAsync(Guid cartId, CancellationToken cancellationToken) =>
        db.Carts
            .Include(cart => cart.Items)
            .SingleOrDefaultAsync(cart => cart.Id == cartId, cancellationToken);

    private ObjectResult CartNotFound() =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Cart not found.");
}
