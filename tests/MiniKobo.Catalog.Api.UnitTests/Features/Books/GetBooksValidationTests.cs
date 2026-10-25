using MiniKobo.Catalog.Api.Features.Books;

namespace MiniKobo.Catalog.Api.UnitTests.Features.Books;

[TestFixture]
public sealed class GetBooksValidationTests
{
    [TestCase(1, 1)]
    [TestCase(1, 20)]
    [TestCase(5, GetBooks.MaxPageSize)]
    public void ValidatePaging_WithValidValues_ReturnsNoErrors(int page, int pageSize)
    {
        var errors = GetBooks.ValidatePaging(page, pageSize);

        Assert.That(errors, Is.Null);
    }

    [TestCase(0, 20)]
    [TestCase(-1, 20)]
    [TestCase(1, 0)]
    [TestCase(1, GetBooks.MaxPageSize + 1)]
    public void ValidatePaging_WithInvalidValues_ReturnsPagingError(int page, int pageSize)
    {
        var errors = GetBooks.ValidatePaging(page, pageSize);

        Assert.That(errors, Is.Not.Null.And.ContainKey("paging"));
    }
}
