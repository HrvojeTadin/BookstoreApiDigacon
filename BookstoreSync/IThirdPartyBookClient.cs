namespace BookstoreSync;
public interface IThirdPartyBookClient
{
    /// <summary>
    /// Simulates a call to an external API and returns at least `count` books.
    /// </summary>
    Task<List<Book>> FetchBooksAsync(int count);
}
