using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using LorAuto.Client.Model;

namespace LorAuto.Client;

/// <summary>
/// Represents the types of requests for the game client API.
/// </summary>
internal enum EGameClientApiRequestType
{
    /// <summary>
    /// ActiveDeck request.
    /// </summary>
    ActiveDeck,

    /// <summary>
    /// CardPositions request.
    /// </summary>
    CardPositions,

    /// <summary>
    /// GameResult request.
    /// </summary>
    GameResult
}

/// <summary>
/// Represents a game client API for interacting with the game client.
/// </summary>
public sealed class GameClientApi : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<EGameClientApiRequestType, string> _requestsMap;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameClientApi"/> class with the specified port.
    /// </summary>
    /// <param name="port">The port number for the game client API.</param>
    public GameClientApi(int port = 21337)
    {
        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}/"),
            Timeout = TimeSpan.FromSeconds(10)
        };

        _requestsMap = new Dictionary<EGameClientApiRequestType, string>()
        {
            { EGameClientApiRequestType.ActiveDeck, "static-decklist" },
            { EGameClientApiRequestType.CardPositions, "positional-rectangles" },
            { EGameClientApiRequestType.GameResult, "game-result" },
        };
    }

    /// <summary>
    /// Sends a GET request to the game client API and retrieves the response as a string.
    /// </summary>
    /// <param name="apiRequestType">The type of API request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The response data as a string.</returns>
    private async Task<string> GetRequestAsync(EGameClientApiRequestType apiRequestType, CancellationToken ct = default)
    {
        return await _httpClient.GetStringAsync(_requestsMap[apiRequestType], ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the active deck information from the game client API.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A tuple containing the active deck response and any exception that occurred during the request.</returns>
    public async Task<(ActiveDeckApiResponse?, Exception?)> GetActiveDeckAsync(CancellationToken ct = default)
    {
        try
        {
            string requestData = await GetRequestAsync(EGameClientApiRequestType.ActiveDeck, ct).ConfigureAwait(false);
            var activeDeckJson = JsonSerializer.Deserialize<JsonObject?>(requestData);

            if (activeDeckJson is null)
                throw new UnreachableException();

            var activeDeck = new ActiveDeckApiResponse()
            {
                DeckCode = activeDeckJson["DeckCode"]?.GetValue<string>(),
                CardsInDeck = activeDeckJson["CardsInDeck"]?.Deserialize<Dictionary<string, int>>() ?? new Dictionary<string, int>()
            };
            return (activeDeck, null);
        }
        catch (Exception e)
        {
            return (null, e);
        }
    }

    /// <summary>
    /// Gets the card positions from the game client API.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A tuple containing the card positions response and any exception that occurred during the request.</returns>
    public async Task<(CardPositionsApiResponse?, Exception?)> GetCardPositionsAsync(CancellationToken ct = default)
    {
        try
        {
            string requestData = await GetRequestAsync(EGameClientApiRequestType.CardPositions, ct).ConfigureAwait(false);
            var cardPositions = JsonSerializer.Deserialize<CardPositionsApiResponse?>(requestData);

            return (cardPositions, null);
        }
        catch (Exception e)
        {
            return (null, e);
        }
    }

    /// <summary>
    /// Gets the game result from the game client API.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A tuple containing the game result response and any exception that occurred during the request.</returns>
    public async Task<(GameResultApiResponse?, Exception?)> GetGameResultAsync(CancellationToken ct = default)
    {
        try
        {
            string requestData = await GetRequestAsync(EGameClientApiRequestType.GameResult, ct).ConfigureAwait(false);
            var gameResult = JsonSerializer.Deserialize<GameResultApiResponse?>(requestData);

            return (gameResult, null);
        }
        catch (Exception e)
        {
            return (null, e);
        }
    }

    /// <summary>
    /// Releases the resources used by the <see cref="GameClientApi"/> instance.
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
