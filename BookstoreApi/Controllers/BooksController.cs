namespace BookstoreApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController(
    IBookstoreService service,
    ILogger<BooksController> logger) : ControllerBase
{
    // GET: api/books
    [HttpGet]
    [Authorize(Policy = AuthPolicies.RequireReadRole)]
    [ProducesResponseType(typeof(IEnumerable<BookDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks(CancellationToken ct)
    {
        logger.LogInformation("Start GET all books");
        var books = await service.GetAllBooksAsync(ct);
        logger.LogInformation("Completed GET all books; retrieved {Count} items", books.Count());
        return Ok(books);
    }

    // GET: api/books/{id}
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthPolicies.RequireReadRole)]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookDto>> GetBookById(int id, CancellationToken ct)
    {
        logger.LogInformation("Start GET book by id {BookId}", id);
        var dto = await service.GetBookByIdAsync(id, ct);
        if (dto is null)
        {
            logger.LogWarning("Book with id {BookId} not found", id);
            return NotFound();
        }
        return Ok(dto);
    }

    // GET: api/books/top-10-rated-books
    [HttpGet("top-10-rated-books")]
    [Authorize(Policy = AuthPolicies.RequireReadRole)]
    [ProducesResponseType(typeof(IEnumerable<TopRatedBookDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TopRatedBookDto>>> GetTopRatedBooks(CancellationToken ct)
    {
        logger.LogInformation("Start GET top 10 rated books");
        var top = await service.GetTop10BooksAsync(ct);
        return Ok(top);
    }

    // POST: api/books
    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireReadWriteRole)]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookDto>> CreateBook(
        [FromBody] CreateBookRequest request,
        CancellationToken ct)
    {
        logger.LogInformation("Start POST create book {@Request}", request);
        try
        {
            var created = await service.CreateBookAsync(request, ct);
            logger.LogInformation("Book created with id {BookId}", created.Id);
            return CreatedAtAction(
                nameof(GetBookById),
                new { id = created.Id },
                created);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error on create book");
            return BadRequest(ex.Message);
        }
    }

    // PUT: api/books/{id}/price
    [HttpPut("{id:int}/price")]
    [Authorize(Policy = AuthPolicies.RequireReadWriteRole)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBookPrice(
        int id,
        [FromBody] UpdateBookPriceRequest req,
        CancellationToken ct)
    {
        logger.LogInformation("Start PUT update price for book {BookId}: {NewPrice}", id, req.NewPrice);
        try
        {
            await service.UpdateBookPriceAsync(id, req.NewPrice, ct);
            logger.LogInformation("Successfully updated price for book {BookId}", id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid price for update book {BookId}", id);
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Book with id {BookId} not found for price update", id);
            return NotFound();
        }
    }

    // DELETE: api/books/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthPolicies.RequireReadWriteRole)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBook(int id, CancellationToken ct)
    {
        logger.LogInformation("Start DELETE book with id {BookId}", id);
        try
        {
            await service.DeleteBookAsync(id, ct);
            logger.LogInformation("Deleted book with id {BookId}", id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Book with id {BookId} not found for deletion", id);
            return NotFound();
        }
    }
}
