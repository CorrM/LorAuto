using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using LorAuto.Client.Model;

namespace LorAuto.Client;

internal enum EGameClientApiRequestType
{
    ActiveDeck,
    CardPositions,
    GameResult
}

public sealed class GameClientApi : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<EGameClientApiRequestType, string> _requestsMap;

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

    private async Task<string> GetRequestAsync(EGameClientApiRequestType apiRequestType, CancellationToken ct = default)
    {
        return await _httpClient.GetStringAsync(_requestsMap[apiRequestType], ct).ConfigureAwait(false);
    }
    
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

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
