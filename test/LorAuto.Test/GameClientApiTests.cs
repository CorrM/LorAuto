using LorAuto.Client;

namespace LorAuto.Test;

public class GameClientApiTests
{
    [Fact]
    public async Task GetActiveDeckAsync_ReturnsActiveDeckAndNoException()
    {
        // Arrange
        var gameClientApi = new GameClientApi();

        // Act
        var (activeDeck, exception) = await gameClientApi.GetActiveDeckAsync();

        // Assert
        Assert.Null(exception);
        Assert.NotNull(activeDeck);
    }

    [Fact]
    public async Task GetCardPositionsAsync_ReturnsCardPositionsAndNoException()
    {
        // Arrange
        var gameClientApi = new GameClientApi();

        // Act
        var (cardPositions, exception) = await gameClientApi.GetCardPositionsAsync();

        // Assert
        Assert.Null(exception);
        Assert.NotNull(cardPositions);
    }

    [Fact]
    public async Task GetGameResultAsync_ReturnsGameResultAndNoException()
    {
        // Arrange
        var gameClientApi = new GameClientApi();
        
        // Act
        var (gameResult, exception) = await gameClientApi.GetGameResultAsync();

        // Assert
        Assert.Null(exception);
        Assert.NotNull(gameResult);
    }
    
    [Fact]
    public async Task GetActiveDeckAsync_ReturnsNullAndException_WhenRequestFails()
    {
        // Arrange
        var gameClientApi = new GameClientApi(12345); // Use a non-existent port to simulate a failed request

        // Act
        var (activeDeck, exception) = await gameClientApi.GetActiveDeckAsync();

        // Assert
        Assert.Null(activeDeck);
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task GetCardPositionsAsync_ReturnsNullAndException_WhenRequestFails()
    {
        // Arrange
        var gameClientApi = new GameClientApi(12345); // Use a non-existent port to simulate a failed request

        // Act
        var (cardPositions, exception) = await gameClientApi.GetCardPositionsAsync();

        // Assert
        Assert.Null(cardPositions);
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task GetGameResultAsync_ReturnsNullAndException_WhenRequestFails()
    {
        // Arrange
        var gameClientApi = new GameClientApi(12345); // Use a non-existent port to simulate a failed request

        // Act
        var (gameResult, exception) = await gameClientApi.GetGameResultAsync();

        // Assert
        Assert.Null(gameResult);
        Assert.NotNull(exception);
    }
}
