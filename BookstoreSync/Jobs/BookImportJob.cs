namespace BookstoreSync.Jobs;

public class BookImportJob(
    BookstoreDigaconDbContext db,
    IThirdPartyBookClient client,
    ILogger<BookImportJob> logger,
    IOptions<BookImportSettings> opts) : IJob
{
    private readonly BookstoreDigaconDbContext _db = db;
    private readonly IThirdPartyBookClient _client = client;
    private readonly ILogger<BookImportJob> _logger = logger;
    private readonly int _threshold = opts.Value.FuzzyThreshold;

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("BookImportJob starting (fuzzy threshold={Threshold})", _threshold);

        // 1) Dohvati simulirane knjige
        var imported = await _client.FetchBooksAsync(100_000);

        // 2) Pripremi postojeće naslove
        var incomingTitles = imported
            .Select(b => b.Title?.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingTitles = await _db.Books
            .Where(b => incomingTitles.Contains(b.Title!))
            .Select(b => b.Title!)
            .ToListAsync();

        // 3) Fuzzy filtriranje
        var toAdd = new List<Book>();
        int skipFuzzy = 0;

        foreach (var book in imported)
        {
            var title = book.Title?.Trim();
            if (string.IsNullOrWhiteSpace(title))
                continue;

            bool isDuplicate = existingTitles.Any(et =>
                ComputeLevenshtein(et, title) <= _threshold);

            if (isDuplicate)
            {
                skipFuzzy++;
            }
            else
            {
                toAdd.Add(book);
            }
        }

        _logger.LogInformation("Skipped {SkipFuzzy} books due to fuzzy duplicates", skipFuzzy);

        // 4) Batch uvoz
        const int batchSize = 2000;
        int totalBatches = (int)Math.Ceiling(toAdd.Count / (double)batchSize);

        for (int i = 0; i < toAdd.Count; i += batchSize)
        {
            var batch = toAdd.Skip(i).Take(batchSize);
            _db.Books.AddRange(batch);
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            _logger.LogDebug(
                "Imported batch {Batch}/{Total} ({Count} items)",
                i / batchSize + 1, totalBatches, batch.Count());
        }

        _logger.LogInformation("BookImportJob finished; imported {ImportedCount} books", toAdd.Count);
    }

    // Standardna Levenshtein implementacija
    public static int ComputeLevenshtein(string a, string b)
    {
        var dp = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) dp[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1,     // brisanje
                             dp[i, j - 1] + 1),    // umetanje
                    dp[i - 1, j - 1] + cost);    // zamjena
            }

        return dp[a.Length, b.Length];
    }
}
