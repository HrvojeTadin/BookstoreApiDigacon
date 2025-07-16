namespace BookstoreApi.DTOs;

public class CreateBookRequest
{
    [Required, MinLength(1)]
    public string Title { get; set; } = default!;

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Required, MinLength(1)]
    public List<string> AuthorNames { get; set; } = [];

    [Required, MinLength(1)]
    public List<string> GenreNames { get; set; } = [];
}
