using System.ComponentModel.DataAnnotations;

namespace MiniKobo.Catalog.Api.Data;

public sealed class RavenDbOptions
{
    public const string SectionName = "RavenDb";

    [Required, MinLength(1)]
    public string[] Urls { get; init; } = [];

    [Required]
    public string Database { get; init; } = string.Empty;
}