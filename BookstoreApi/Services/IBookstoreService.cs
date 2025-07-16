namespace BookstoreApi.Services;

public interface IBookstoreService
{
    Task<IEnumerable<BookDto>> GetAllBooksAsync(CancellationToken ct);
    Task<BookDto?> GetBookByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<TopRatedBookDto>> GetTop10BooksAsync(CancellationToken ct);
    Task<BookDto> CreateBookAsync(CreateBookRequest request, CancellationToken ct);
    Task UpdateBookPriceAsync(int id, decimal newPrice, CancellationToken ct);
    Task DeleteBookAsync(int id, CancellationToken ct);
}
