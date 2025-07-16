namespace BookstoreSync.Entities;

public class Genre
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public ICollection<Book> Books { get; private set; } = [];

    private Genre() { }

    public Genre(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Name = name.Trim();
    }
}
