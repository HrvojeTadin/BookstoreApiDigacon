namespace BookstoreSync.Configuration;

public class BookImportSettings
{
    /// <summary>
    /// Maximalna Levenshtein udaljenost za koju smatramo da je riječ o istom naslovu.
    /// </summary>
    public int FuzzyThreshold { get; set; } = 2;
}
