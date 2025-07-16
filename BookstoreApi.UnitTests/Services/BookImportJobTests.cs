namespace BookstoreApi.UnitTests.Services;

public class BookImportJobTests
{
    private const int FuzzyThreshold = 2;

    [Fact]
    public void ComputeLevenshtein_CorrectDistance()
    {
        string a = "Crime and punishment";
        string b = "Criem and punishment";

        int dist = BookImportJob.ComputeLevenshtein(a, b);

        Assert.Equal(2, dist);
        Assert.InRange(dist, 0, FuzzyThreshold);
    }

    [Fact]
    public void FilterFuzzy_SkipsTypoAndExact_DoesKeepNew()
    {
        var existing = new List<string> { "Crime and punishment" };
        var imported = new List<Book>
        {
            new("Crime and punishment", 10m),        // exact duplicate → skip
            new("Criem and punishment", 10m),        // fuzzy duplicate → skip
            new("A Completely New Book", 15.5m)      // new → keep
        };

        var toAdd = imported
             .Where(b =>
             {
                 var title = b.Title.Trim();

                 if (existing.Any(e =>
                     string.Equals(e, title, StringComparison.OrdinalIgnoreCase)))
                     return false;

                 bool fuzzy = existing.Any(e =>
                     BookImportJob.ComputeLevenshtein(e, title) <= FuzzyThreshold);

                 return !fuzzy;
             })
             .Select(b => b.Title)
             .ToList();

        Assert.Single(toAdd);
        Assert.Contains("A Completely New Book", toAdd);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void FuzzyThreshold_VariousThresholds(int threshold, bool expectWithinThreshold)
    {
        string existing = "Crime and punishment";
        string typo = "Criem and punishment";

        int dist = BookImportJob.ComputeLevenshtein(existing, typo);

        if (expectWithinThreshold)
            Assert.True(dist <= threshold);
        else
            Assert.True(dist > threshold);
    }
}
