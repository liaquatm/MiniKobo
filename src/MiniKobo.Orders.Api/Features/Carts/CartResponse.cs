namespace MiniKobo.Orders.Api.Features.Carts;

public sealed record CartResponse(Guid Id, IReadOnlyList<CartItemResponse> Items, decimal Total)
{
    public static CartResponse FromCart(Cart cart) =>
        new(cart.Id, cart.Items.Select(CartItemResponse.FromItem).ToList(), cart.Total);
}

public sealed record CartItemResponse(string Isbn, string Title, decimal UnitPrice, int Quantity, decimal LineTotal)
{
    public static CartItemResponse FromItem(CartItem item) =>
        new(item.Isbn, item.Title, item.UnitPrice, item.Quantity, item.LineTotal);
}
