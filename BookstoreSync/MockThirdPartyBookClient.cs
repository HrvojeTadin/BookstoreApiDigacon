namespace BookstoreSync;

public class MockThirdPartyBookClient : IThirdPartyBookClient
{
    public Task<List<Book>> FetchBooksAsync(int count)
    {
        var list = Enumerable.Range(0, count)
                 .Select(i => new Book($"Book {i}", 9.99m + (i % 10)))
                 .ToList();

        return Task.FromResult(list);
    }
}
