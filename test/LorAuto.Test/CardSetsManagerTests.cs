using LorAuto.Client;

namespace LorAuto.Test;

public class CardSetsManagerTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private const string CARD_SETS_DIR_NAME = "TestCardSets";

    public CardSetsManagerTests()
    {
        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri("https://dd.b.pvp.net/latest/")
        };
    }

    private static string GetCardSetsBasePath()
    {
        return Path.Combine(Environment.CurrentDirectory, CARD_SETS_DIR_NAME);
    }

    [Fact]
    public async Task DeleteCardSets_WhenCalled_ShouldDeleteCardSetsDirectory()
    {
        // Arrange
        var cardSetManager = new CardSetsManager(CARD_SETS_DIR_NAME);

        // Act
        await cardSetManager.DownloadMissingCardSetsAsync();
        bool result = cardSetManager.DeleteCardSets();

        // Assert
        Assert.True(result);
        Assert.False(Directory.Exists(GetCardSetsBasePath()));
    }

    [Fact]
    public async Task DownloadMissingCardSetsAsync_WhenCalled_ShouldDownloadMissingCardSets()
    {
        // Arrange
        var cardSetManager = new CardSetsManager(CARD_SETS_DIR_NAME);

        // Act
        await cardSetManager.DownloadMissingCardSetsAsync();

        // Assert
        string[] cardSetFiles = Directory.GetFiles(GetCardSetsBasePath());
        Assert.NotEmpty(cardSetFiles);
    }

    public void Dispose()
    {
        // Clean up (delete card sets directory if exists)
        string cardSetsPath = GetCardSetsBasePath();
        if (Directory.Exists(cardSetsPath))
            Directory.Delete(cardSetsPath, true);
    }
}
