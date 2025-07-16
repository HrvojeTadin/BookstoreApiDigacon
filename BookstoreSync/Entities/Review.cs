namespace BookstoreSync.Entities;

public class Review
{
    public int Id { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int Rating { get; private set; }

    public int BookId { get; private set; }
    public Book Book { get; private set; } = null!;

    private Review() { }

    public Review(string description, int rating)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");

        Description = description.Trim();
        Rating = rating;
    }
}
