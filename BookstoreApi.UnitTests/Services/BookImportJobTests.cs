namespace BookstoreApi.UnitTests.Services;

public class BookImportJobTests
{
    // Prag koji koristimo u implementaciji (po defaultu 2)
    private const int FuzzyThreshold = 2;

    [Fact]
    public void ComputeLevenshtein_CorrectDistance()
    {
        // arrange
        string a = "Crime and punishment";
        string b = "Criem and punishment";

        // act
        int dist = BookImportJob.ComputeLevenshtein(a, b);

        // assert
        Assert.Equal(2, dist);
        Assert.InRange(dist, 0, FuzzyThreshold);
    }

    [Fact]
    public void FilterFuzzy_SkipsTypoAndExact_DoesKeepNew()
    {
        // arrange
        var existing = new List<string> { "Crime and punishment" };
        var imported = new List<Book>
        {
            new("Crime and punishment", 10m),        // exact duplicate → skip
            new("Criem and punishment", 10m),        // fuzzy duplicate → skip
            new("A Completely New Book", 15.5m)      // new → keep
        };

        // act: repliciramo logiku iz Job-a
        var toAdd = imported
            .Where(b =>
            {
                var title = b.Title.Trim();

                // 1) exact skip
                if (existing.Any(e =>
                    string.Equals(e, title, StringComparison.OrdinalIgnoreCase)))
                    return false;

                // 2) fuzzy skip
                bool fuzzy = existing.Any(e =>
                    BookImportJob.ComputeLevenshtein(e, title) <= FuzzyThreshold);

                return !fuzzy;
            })
            .Select(b => b.Title)
            .ToList();

        // assert
        Assert.Single(toAdd);
        Assert.Contains("A Completely New Book", toAdd);
    }

    [Theory]
    [InlineData(0, false)]  // threshold 0 → ni jedan fuzzy ne prolazi
    [InlineData(1, false)]  // threshold 1 → dist(2) > 1 → skip
    [InlineData(2, true)]   // threshold 2 → dist(2) <= 2 → fuzzy bi bio skip
    [InlineData(3, true)]   // threshold 3 → dist(2) <= 3 → fuzzy bi bio skip
    public void FuzzyThreshold_VariousThresholds(int threshold, bool expectWithinThreshold)
    {
        // arrange
        string existing = "Crime and punishment";
        string typo = "Criem and punishment";

        // compute once
        int dist = BookImportJob.ComputeLevenshtein(existing, typo);

        // act & assert
        if (expectWithinThreshold)
            Assert.True(dist <= threshold);
        else
            Assert.True(dist > threshold);
    }
}
