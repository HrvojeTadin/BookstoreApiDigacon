namespace BookstoreSync;
public interface IThirdPartyBookClient
{
    Task<List<Book>> FetchBooksAsync(int count);
}
