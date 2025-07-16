namespace BookstoreApi.DTOs;

public record BookDto(
    int Id,
    string Title,
    List<string> Authors,
    List<string> Genres,
    decimal Price,
    double AverageRating
);
