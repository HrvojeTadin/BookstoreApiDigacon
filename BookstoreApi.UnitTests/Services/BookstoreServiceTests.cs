namespace BookstoreApi.UnitTests.Services;

public class BookstoreServiceTests
{
    private BookstoreDigaconDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<BookstoreDigaconDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BookstoreDigaconDbContext(options);
    }

    [Fact]
    public async Task CreateBookAsync_Should_CreateBook_When_ValidRequest()
    {
        var db = CreateInMemoryContext();
        var logger = NullLogger<BookstoreService>.Instance;
        var service = new BookstoreService(db, logger);

        var request = new CreateBookRequest
        {
            Title = "Test Book",
            Price = 10m,
            AuthorNames = ["New Author"],
            GenreNames = ["New Genre"]
        };

        var dto = await service.CreateBookAsync(request, CancellationToken.None);

        Assert.Equal("Test Book", dto.Title);
        Assert.Single(dto.Authors);
        Assert.Equal("New Author", dto.Authors[0]);
        Assert.Single(dto.Genres);
        Assert.Equal("New Genre", dto.Genres[0]);
        Assert.Equal(10m, dto.Price);
        Assert.Equal(0, dto.AverageRating);

        var bookInDb = await db.Books
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .FirstAsync();
        Assert.Equal("Test Book", bookInDb.Title);
    }

    [Theory]
    [InlineData("", 10, new[] { "A" }, new[] { "G" })] // missing title
    [InlineData("T", 0, new[] { "A" }, new[] { "G" })] // invalid price
    [InlineData("T", 10, new string[0], new[] { "G" })] // no authors
    [InlineData("T", 10, new[] { "A" }, new string[0])] // no genres
    public async Task CreateBookAsync_Should_ThrowArgumentException_When_Invalid(
        string title,
        decimal price,
        string[] authors,
        string[] genres)
    {
        var db = CreateInMemoryContext();
        var logger = NullLogger<BookstoreService>.Instance;
        var service = new BookstoreService(db, logger);

        var request = new CreateBookRequest
        {
            Title = title,
            Price = price,
            AuthorNames = [.. authors],
            GenreNames = [.. genres]
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateBookAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllBooksAsync_ReturnsCorrectAverageRating()
    {
        var db = CreateInMemoryContext();
        var logger = NullLogger<BookstoreService>.Instance;
        var service = new BookstoreService(db, logger);

        var author = new Author("A", 1970);
        var genre = new Genre("G");
        db.Authors.Add(author);
        db.Genres.Add(genre);
        await db.SaveChangesAsync();

        var book = new Book("B", 10m);
        book.Authors.Add(author);
        book.Genres.Add(genre);
        book.Reviews.Add(new Review("r1", 5));
        book.Reviews.Add(new Review("r2", 3));
        db.Books.Add(book);
        await db.SaveChangesAsync();

        var list = (await service.GetAllBooksAsync(CancellationToken.None)).ToList();

        Assert.Single(list);
        Assert.Equal(4, list[0].AverageRating);
    }

    [Fact]
    public async Task GetBookByIdAsync_ReturnsDto_WhenExists()
    {
        var db = CreateInMemoryContext();
        var logger = NullLogger<BookstoreService>.Instance;
        var service = new BookstoreService(db, logger);

        var author = new Author("A", 1970);
        var genre = new Genre("G");
        db.Authors.Add(author);
        db.Genres.Add(genre);
        await db.SaveChangesAsync();

        var book = new Book("X", 5m);
        book.Authors.Add(author);
        book.Genres.Add(genre);
        db.Books.Add(book);
        await db.SaveChangesAsync();

        var dto = await service.GetBookByIdAsync(book.Id, CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal("X", dto!.Title);
        Assert.Contains("A", dto.Authors);
        Assert.Contains("G", dto.Genres);
    }

    [Fact]
    public async Task GetBookByIdAsync_ReturnsNull_WhenNotExists()
    {
        var service = new BookstoreService(CreateInMemoryContext(), NullLogger<BookstoreService>.Instance);

        var dto = await service.GetBookByIdAsync(12345, CancellationToken.None);

        Assert.Null(dto);
    }

    [Fact]
    public async Task UpdateBookPriceAsync_UpdatesPrice_WhenValid()
    {
        var db = CreateInMemoryContext();
        var logger = NullLogger<BookstoreService>.Instance;
        var service = new BookstoreService(db, logger);

        var author = new Author("A", 1970);
        var genre = new Genre("G");
        db.Authors.Add(author);
        db.Genres.Add(genre);
        await db.SaveChangesAsync();

        var book = new Book("T", 1m);
        book.Authors.Add(author);
        book.Genres.Add(genre);
        db.Books.Add(book);
        await db.SaveChangesAsync();

        await service.UpdateBookPriceAsync(book.Id, 12.34m, CancellationToken.None);

        var updated = await db.Books.FindAsync(book.Id);
        Assert.Equal(12.34m, updated!.Price);
    }

    [Fact]
    public async Task UpdateBookPriceAsync_ThrowsArgumentException_WhenInvalidPrice()
    {
        var service = new BookstoreService(CreateInMemoryContext(), NullLogger<BookstoreService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateBookPriceAsync(1, -1m, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateBookPriceAsync_ThrowsKeyNotFound_WhenNotExists()
    {
        var service = new BookstoreService(CreateInMemoryContext(), NullLogger<BookstoreService>.Instance);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateBookPriceAsync(9999, 5m, CancellationToken.None));
    }

    [Fact]
    public async Task CreateBookAsync_DoesNotDuplicateAuthorsOrGenres()
    {
        var db = CreateInMemoryContext();
        var logger = NullLogger<BookstoreService>.Instance;
        var service = new BookstoreService(db, logger);

        var request = new CreateBookRequest
        {
            Title = "Test",
            Price = 2.5m,
            AuthorNames = ["Same"],
            GenreNames = ["SameG"]
        };

        await service.CreateBookAsync(request, CancellationToken.None);
        await service.CreateBookAsync(request, CancellationToken.None);

        Assert.Single(db.Authors);
        Assert.Single(db.Genres);
    }
}
