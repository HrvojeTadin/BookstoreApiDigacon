namespace BookstoreSync.Entities;

public class Book
{
    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public decimal Price { get; private set; }

    public ICollection<Author> Authors { get; private set; } = [];
    public ICollection<Genre> Genres { get; private set; } = [];
    public ICollection<Review> Reviews { get; private set; } = [];

    private Book() { }

    public Book(string title, decimal price)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if (price <= 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be positive.");

        Title = title.Trim();
        Price = price;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentException("Price must be greater than 0.", nameof(newPrice));

        Price = newPrice;
    }
}
