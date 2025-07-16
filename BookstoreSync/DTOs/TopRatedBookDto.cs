namespace BookstoreSync.DTOs;

public record TopRatedBookDto(
    int Id,
    string Title,
    double AverageRating
);
