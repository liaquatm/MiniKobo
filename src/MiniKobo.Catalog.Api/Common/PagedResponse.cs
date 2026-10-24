namespace MiniKobo.Catalog.Api.Common;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount);