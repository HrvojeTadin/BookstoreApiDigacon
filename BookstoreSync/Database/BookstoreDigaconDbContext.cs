namespace BookstoreSync.Database;

public class BookstoreDigaconDbContext(DbContextOptions<BookstoreDigaconDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<TopRatedBookDto> TopRatedBookDtos => Set<TopRatedBookDto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Implicit many-to-many (Authors <-> Books)
        modelBuilder.Entity<Book>()
            .HasMany(b => b.Authors)
            .WithMany(a => a.Books);

        // Implicit many-to-many (Genres <-> Books)
        modelBuilder.Entity<Book>()
            .HasMany(b => b.Genres)
            .WithMany(g => g.Books);

        modelBuilder.Entity<Book>()
            .Property(b => b.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Review>()
            .Property(r => r.Rating)
            .IsRequired();

        modelBuilder.Entity<Author>()
            .Property(a => a.Name)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Book>()
            .Property(b => b.Title)
            .HasMaxLength(200)
            .IsRequired();

        modelBuilder.Entity<Genre>()
            .Property(g => g.Name)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder
            .Entity<TopRatedBookDto>()
            .HasNoKey()
            .ToView(null);
    }
}
