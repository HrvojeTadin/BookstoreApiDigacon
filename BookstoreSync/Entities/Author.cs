namespace BookstoreSync.Entities;

public class Author
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int YearOfBirth { get; private set; }

    public ICollection<Book> Books { get; private set; } = [];

    private Author() { }

    public Author(string name, int yearOfBirth)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Name = name.Trim();
        YearOfBirth = yearOfBirth;
    }
}
