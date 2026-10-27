using System.ComponentModel.DataAnnotations;

namespace MiniKobo.Orders.Api.Features.Carts;

public sealed record SetCartItemRequest([Range(Cart.MinQuantity, Cart.MaxQuantity)] int Quantity);
