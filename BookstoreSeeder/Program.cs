using BookstoreSync.Database;
using BookstoreSync.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BookstoreSeeder
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(cfg =>
                {
                    cfg.SetBasePath(Environment.CurrentDirectory)
                       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContext<BookstoreDigaconDbContext>(opts =>
                        opts.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));
                })
                .Build();

            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BookstoreDigaconDbContext>();

            Console.Write("Do you want to clear existing data first? (y/n): ");
            if (Console.ReadLine()?.Trim().ToLowerInvariant() == "y")
            {
                await ClearDataWithEfAsync(db);
            }

            Console.Write("\nDo you want to seed the database now? (y/n): ");
            if (Console.ReadLine()?.Trim().ToLowerInvariant() != "y")
            {
                Console.WriteLine("Seeding cancelled by user.");
                return;
            }

            Console.WriteLine("\nApplying migrations...");
            await db.Database.MigrateAsync();

            Console.WriteLine("Seeding initial data...");
            await SeedDataAsync(db);

            Console.WriteLine("\nSeeding completed. Press any key to exit.");
            Console.ReadKey();
        }

        private static async Task ClearDataWithEfAsync(BookstoreDigaconDbContext db)
        {
            Console.WriteLine("\nClearing existing data...");

            await db.Reviews.ExecuteDeleteAsync();
            Console.WriteLine("Reviews cleared");

            await db.Books.ExecuteDeleteAsync();
            Console.WriteLine("Books cleared");

            await db.Authors.ExecuteDeleteAsync();
            Console.WriteLine("Authors cleared");

            await db.Genres.ExecuteDeleteAsync();
            Console.WriteLine("Genres cleared");
        }

        private static async Task SeedDataAsync(BookstoreDigaconDbContext db)
        {
            if (!await db.Authors.AnyAsync())
            {
                db.Authors.AddRange(
                    new Author("George Orwell", 1903),
                    new Author("Jane Austen", 1775),
                    new Author("Fyodor Dostoevsky", 1821)
                );
                await db.SaveChangesAsync();
                Console.WriteLine("Authors seeded");
            }
            else
            {
                Console.WriteLine("Authors already exist, skipping");
            }

            if (!await db.Genres.AnyAsync())
            {
                db.Genres.AddRange(
                    new Genre("Dystopia"),
                    new Genre("Romance"),
                    new Genre("Philosophical Fiction")
                );
                await db.SaveChangesAsync();
                Console.WriteLine("Genres seeded");
            }
            else
            {
                Console.WriteLine("Genres already exist, skipping");
            }

            if (!await db.Books.AnyAsync())
            {
                var orwell = await db.Authors.SingleAsync(a => a.Name == "George Orwell");
                var dystopia = await db.Genres.SingleAsync(g => g.Name == "Dystopia");
                var austen = await db.Authors.SingleAsync(a => a.Name == "Jane Austen");
                var romance = await db.Genres.SingleAsync(g => g.Name == "Romance");
                var dostoevsky = await db.Authors.SingleAsync(a => a.Name == "Fyodor Dostoevsky");
                var philosophical = await db.Genres.SingleAsync(g => g.Name == "Philosophical Fiction");

                var b1 = new Book("1984", 9.99m);
                b1.Authors.Add(orwell);
                b1.Genres.Add(dystopia);
                b1.Reviews.Add(new Review("A chilling portrayal of totalitarianism", 5));
                b1.Reviews.Add(new Review("Still relevant today", 4));
                db.Books.Add(b1);
                Console.WriteLine("Book '1984' seeded with reviews");

                var b2 = new Book("Pride and Prejudice", 12.50m);
                b2.Authors.Add(austen);
                b2.Genres.Add(romance);
                b2.Reviews.Add(new Review("A timeless classic", 5));
                db.Books.Add(b2);
                Console.WriteLine("Book 'Pride and Prejudice' seeded with review");

                var b3 = new Book("Crime and Punishment", 14.99m);
                b3.Authors.Add(dostoevsky);
                b3.Genres.Add(philosophical);
                b3.Reviews.Add(new Review("Profound psychological insight", 5));
                db.Books.Add(b3);
                Console.WriteLine("Book 'Crime and Punishment' seeded with review");

                await db.SaveChangesAsync();
            }
            else
            {
                Console.WriteLine("Books already exist, skipping");
            }
        }
    }
}
