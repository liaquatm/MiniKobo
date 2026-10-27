using System.ComponentModel.DataAnnotations;

namespace MiniKobo.Orders.Api.Clients.Catalog;

public sealed class CatalogOptions
{
    public const string SectionName = "Catalog";

    [Required, Url]
    public string BaseUrl { get; init; } = string.Empty;
}
