namespace BookstoreApi.Services;

public class BookstoreService(
    BookstoreDigaconDbContext context,
    ILogger<BookstoreService> logger) : IBookstoreService
{
    public async Task<IEnumerable<BookDto>> GetAllBooksAsync(CancellationToken ct)
    {
        logger.LogInformation("Fetching all books");

        var result = await context.Books
            .AsNoTracking()
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .Include(b => b.Reviews)
            .Select(b => new BookDto(
                b.Id,
                b.Title,
                b.Authors.Select(a => a.Name).ToList(),
                b.Genres.Select(g => g.Name).ToList(),
                b.Price,
                b.Reviews.Any() ? b.Reviews.Average(r => r.Rating) : 0
            ))
            .ToListAsync(ct);

        logger.LogInformation("Fetched {Count} books", result.Count);

        return result;
    }

    public async Task<BookDto?> GetBookByIdAsync(int id, CancellationToken ct)
    {
        logger.LogInformation("Fetching book by id {BookId}", id);

        var book = await context.Books
            .AsNoTracking()
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .Include(b => b.Reviews)
            .Where(b => b.Id == id)
            .Select(b => new BookDto(
                b.Id,
                b.Title,
                b.Authors.Select(a => a.Name).ToList(),
                b.Genres.Select(g => g.Name).ToList(),
                b.Price,
                b.Reviews.Any() ? b.Reviews.Average(r => r.Rating) : 0
            ))
            .FirstOrDefaultAsync(ct);

        return book;
    }

    public async Task<IEnumerable<TopRatedBookDto>> GetTop10BooksAsync(CancellationToken ct)
    {
        logger.LogInformation("Fetching top 10 rated books");

        const string sql = @"
                SELECT TOP 10 b.Id, b.Title, AVG(CAST(r.Rating AS FLOAT)) AS AverageRating
                FROM Books b
                JOIN Reviews r ON b.Id = r.BookId
                GROUP BY b.Id, b.Title
                ORDER BY AverageRating DESC";

        var top = await context.TopRatedBookDtos
            .FromSqlRaw(sql)
            .AsNoTracking()
            .ToListAsync(ct);

        return top;
    }

    public async Task<BookDto> CreateBookAsync(CreateBookRequest request, CancellationToken ct)
    {
        logger.LogInformation("Creating book {@Request}", request);

        ValidateCreateRequest(request.Title, request.Price, request.AuthorNames, request.GenreNames);

        var authors = await FindOrCreateAuthorsAsync(request.AuthorNames, ct);
        var genres = await FindOrCreateGenresAsync(request.GenreNames, ct);

        var book = new Book(request.Title, request.Price);

        foreach (var a in authors)
        {
            book.Authors.Add(a);
        }
        foreach (var g in genres)
        {
            book.Genres.Add(g);
        }

        context.Books.Add(book);

        await context.SaveChangesAsync(ct);

        return new BookDto(
            book.Id,
            book.Title,
            [.. authors.Select(a => a.Name)],
            [.. genres.Select(g => g.Name)],
            book.Price,
            0  // no reviews yet
        );
    }

    public async Task UpdateBookPriceAsync(int id, decimal newPrice, CancellationToken ct)
    {
        logger.LogInformation("Updating price for book {BookId} to {NewPrice}", id, newPrice);

        if (newPrice <= 0)
            throw new ArgumentException("New price must be > 0.", nameof(newPrice));

        var book = await context.Books.FirstOrDefaultAsync(b => b.Id == id, ct);

        if (book == null)
        {
            logger.LogWarning("Book with id {BookId} not found", id);
            throw new KeyNotFoundException($"Book {id} not found.");
        }

        book.UpdatePrice(newPrice);

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Book {BookId} price updated to {NewPrice}", id, newPrice);
    }

    public async Task DeleteBookAsync(int id, CancellationToken ct)
    {
        var book = await context.Books.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new KeyNotFoundException($"Book {id} not found");

        context.Books.Remove(book);
        await context.SaveChangesAsync(ct);
    }

    private static void ValidateCreateRequest(
        string title,
        decimal price,
        List<string> authorNames,
        List<string> genreNames)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (price <= 0)
            throw new ArgumentException("Price must be > 0.", nameof(price));
        if (authorNames == null || authorNames.Count == 0)
            throw new ArgumentException("At least one author is required.", nameof(authorNames));
        if (genreNames == null || genreNames.Count == 0)
            throw new ArgumentException("At least one genre is required.", nameof(genreNames));
    }

    private async Task<List<Author>> FindOrCreateAuthorsAsync(IEnumerable<string> names, CancellationToken ct)
    {
        var distinct = names
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existing = await context.Authors
            .Where(a => distinct.Contains(a.Name))
            .ToListAsync(ct);

        var toCreate = distinct
            .Except(existing.Select(a => a.Name), StringComparer.OrdinalIgnoreCase)
            .Select(n => new Author(n, yearOfBirth: 0))
            .ToList();

        context.Authors.AddRange(toCreate);

        return existing.Concat(toCreate).ToList();
    }

    private async Task<List<Genre>> FindOrCreateGenresAsync(IEnumerable<string> genre, CancellationToken ct)
    {
        var distinct = genre
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existing = await context.Genres
            .Where(g => distinct.Contains(g.Name))
            .ToListAsync(ct);

        var toCreate = distinct
            .Except(existing.Select(g => g.Name), StringComparer.OrdinalIgnoreCase)
            .Select(n => new Genre(n))
            .ToList();

        context.Genres.AddRange(toCreate);

        return existing.Concat(toCreate).ToList();
    }
}
