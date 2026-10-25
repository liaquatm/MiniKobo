using Raven.Client.Documents.Indexes;

namespace MiniKobo.Catalog.Api.Features.Books;

public sealed class Books_Search : AbstractIndexCreationTask<Book, Books_Search.Result>
{
    public sealed class Result
    {
        public string[] Query { get; set; } = [];
        public string Title { get; set; } = string.Empty;
    }

    public Books_Search()
    {
        Map = books => from book in books
                       select new Result
                       {
                           Query = new[] { book.Title, book.Author, book.Description },
                           Title = book.Title
                       };

        Index(result => result.Query, FieldIndexing.Search);
    }
}