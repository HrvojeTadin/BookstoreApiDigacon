namespace BookstoreApi.IntegrationTests;

public class BooksControllerTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BooksControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClientWithDefaults();

        // ***** OČISTI I PONIŠTI BAZU *****
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookstoreDigaconDbContext>();

        // Uvijek brišemo i ponovo kreiramo strukturu
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        // ***** SEED initial data *****
        // Autori i Žanrovi
        var author = new Author("Seed Author", 1970);
        var genre = new Genre("Seed Genre");
        db.Authors.Add(author);
        db.Genres.Add(genre);
        db.SaveChanges();

        // Knjiga s recenzijama
        var book = new Book("Seeded Book", 20m);
        book.Authors.Add(author);
        book.Genres.Add(genre);
        book.Reviews.Add(new Review("Great", 5));
        book.Reviews.Add(new Review("Good", 4));
        db.Books.Add(book);
        db.SaveChanges();
    }

    [Fact]
    public async Task GetAllBooks_ReturnsSeededBook()
    {
        var list = await _client.GetFromJsonAsync<List<BookDto>>("/api/books");
        Assert.NotNull(list);
        Assert.Single(list);
        Assert.Equal("Seeded Book", list![0].Title);
    }

    [Fact]
    public async Task GetBookById_ReturnsCorrectBook()
    {
        var all = await _client.GetFromJsonAsync<List<BookDto>>("/api/books");
        var id = all![0].Id;
        var resp = await _client.GetAsync($"/api/books/{id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<BookDto>();
        Assert.Equal("Seeded Book", dto!.Title);
    }

    [Fact]
    public async Task GetBookById_NotFound()
    {
        var resp = await _client.GetAsync("/api/books/9999");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task CreateUpdateAndVerifyBookFlow()
    {
        // 1) Create
        var createReq = new CreateBookRequest
        {
            Title = "New Integration Book",
            Price = 11.11m,
            AuthorNames = ["New Author"],
            GenreNames = ["New Genre"]
        };
        var createResp = await _client.PostAsJsonAsync("/api/books", createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<BookDto>();
        Assert.Equal("New Integration Book", created!.Title);

        // 2) GET all => sad ima 2 knjige
        var all = await _client.GetFromJsonAsync<List<BookDto>>("/api/books");
        Assert.Equal(2, all!.Count);

        // 3) Update price
        var id = created.Id;
        var putResp = await _client.PutAsJsonAsync($"/api/books/{id}/price", new { newPrice = 22.22m });
        Assert.Equal(HttpStatusCode.NoContent, putResp.StatusCode);

        // 4) Verify price
        var allBooks = await _client.GetFromJsonAsync<List<BookDto>>("/api/books");
        Assert.NotNull(allBooks);

        // pronađi točno onaj s odgovarajućim ID‑jem
        var updated = allBooks.SingleOrDefault(b => b.Id == id);
        Assert.NotNull(updated);
        Assert.Equal(22.22m, updated!.Price);
    }
}
